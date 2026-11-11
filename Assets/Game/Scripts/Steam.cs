using Mirror;
using Steamworks;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game
{
	public class Steam : SteamManager
	{
		public static Action OnLobbyJoin;
		public static Action OnLobbyLeave;

		private CallResult<LobbyMatchList_t> m_LobbyMatchList;
		private CallResult<LobbyCreated_t> m_LobbyCreated;
		private CallResult<LobbyEnter_t> m_LobbyEntered;

		[Header("UI")]
		[SerializeField] private Button buttonPrefab;
		[SerializeField] private Button createLobbyButton;
		[SerializeField] private Button getLobbyListButton;
		[SerializeField] private Transform buttonContainer;
		[SerializeField] private GameObject lobbyBrowserContainer;

		public CSteamID ActiveLobbyId => activeLobbyId;
		private CSteamID activeLobbyId;

		private IEnumerator Start()
		{
			createLobbyButton.interactable = false;
			getLobbyListButton.interactable = false;
			Debug.Log(SteamManager.Initialized ?
				"Steam init success. Hello " + SteamFriends.GetPersonaName() + " - Friends: " + SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll) :
				"Steam init failed. Log into Steam and try again.");

			while (!SteamManager.Initialized)
			{
				yield return new WaitForSeconds(1f);
			}

			createLobbyButton.interactable = true;
			getLobbyListButton.interactable = true;

			// Create hook for callresult
			m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
			m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
			m_LobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEnetered);
		}

		protected override void Awake()
		{
			base.Awake();
			OnLobbyJoin += HandleLobbyJoin;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			OnLobbyJoin = null;
		}

		public void FindLobbies()
		{
			Debug.Log("Requesting lobby list from Steam...");

			SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
			//SteamMatchmaking.AddRequestLobbyListStringFilter("gameVersion", "1.0", ELobbyComparison.k_ELobbyComparisonEqual);
			//SteamMatchmaking.AddRequestLobbyListNumericalFilter("slotsAvailable", 1, ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan);

			// Clear list
			foreach (Transform t in buttonContainer.transform)
			{
				if(t.gameObject != createLobbyButton.gameObject && t.gameObject != getLobbyListButton.gameObject)
					Destroy(t.gameObject);
			}

			// Make API call
			SteamAPICall_t requestLobbyAPICall = SteamMatchmaking.RequestLobbyList();

			// Register call to CallResult
			m_LobbyMatchList.Set(requestLobbyAPICall);
		}

		// Called when lobby list recieved
		private void OnLobbyMatchList(LobbyMatchList_t callback, bool bIOFailure)
		{
			if (bIOFailure)
			{
				Debug.LogError("Failed to receive lobby list due to IO failure.");
				return;
			}

			// Print out lobby info
			for (int i = 0; i < callback.m_nLobbiesMatching; i++)
			{
				// csteamid is used to ID lobbies and players on Steam
				CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

				// These functions get common data which all lobbies have
				int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
				int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

				// GetLobbyData allows you to fetch custom lobby data, often used for version numbers, etc.
				string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
				string bobcat = SteamMatchmaking.GetLobbyData(lobbyId, "bobcat");
				bobcat = string.IsNullOrEmpty(bobcat) ? "No" : "Yes";

				Debug.Log($"Lobby [{i}]\n   ID: {lobbyId.m_SteamID} | Name: {lobbyName} | Players: {memberCount}/{maxMembers} | Bobcat: {bobcat}\n");

				Button button = Instantiate(buttonPrefab, buttonContainer);
				button.onClick.AddListener(() => JoinLobby(lobbyId));
				button.GetComponentInChildren<TMP_Text>().text = $"<size=+10><b>{lobbyName}</b></size>\nPlayers: {memberCount}/{maxMembers}\nBobcat: {bobcat}";
				
				// darken non-bobcats
				if(bobcat == "No")
				{
					button.image.color = new Color(button.image.color.r, button.image.color.g, button.image.color.b, 0.1f);
					button.GetComponentInChildren<TMP_Text>().color = Color.white;
				}

				// prevent joining own lobby
				if(SteamUser.GetSteamID() == SteamMatchmaking.GetLobbyOwner(lobbyId))
				{
					button.interactable = false;
					button.GetComponentInChildren<TMP_Text>().color = Color.limeGreen;
				}
			}
		}

		public void CreateLobby()
		{
			Debug.Log("Creating lobby...");
			SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10);
			m_LobbyCreated.Set(handle);
			createLobbyButton.interactable = false;
		}

		private void OnLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
		{
			if (bIOFailure || callback.m_eResult != EResult.k_EResultOK)
			{
				Debug.LogError($"Failed to create lobby. Result: {callback.m_eResult}");
				return;
			}

			activeLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
			Debug.Log($"Lobby created successfully! Lobby ID: {activeLobbyId}");

			// Set the custom display name
			SteamMatchmaking.SetLobbyData(activeLobbyId, "name", SteamFriends.GetPersonaName() + "'s Lobby");

			// Required for Mirror + FizzySteamworks: Store host's Steam 64-bit ID so joiners can connect
			string hostSteamID = SteamUser.GetSteamID().ToString();
			SteamMatchmaking.SetLobbyData(activeLobbyId, "HostAddress", hostSteamID);

			// Add custom "bobcat" data to find Bobcat servers
			SteamMatchmaking.SetLobbyData(activeLobbyId, "bobcat", "Yes");

			OnLobbyJoin?.Invoke();
		}

		private void JoinLobby(CSteamID lobbyid)
		{
			Debug.Log("Joining " + lobbyid.m_SteamID);
			SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyid);
			m_LobbyEntered.Set(handle);
			buttonContainer.gameObject.SetActive(false);
		}

		private void OnLobbyEnetered(LobbyEnter_t callback, bool bIOFailure)
		{
			if (bIOFailure)
			{
				Debug.LogError("Join lobby fail");
				buttonContainer.gameObject.SetActive(true);
				return;
			}
			activeLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
			Debug.Log($"Entered lobby: {activeLobbyId}");

			// get host address to join game
			string hostAddress = SteamMatchmaking.GetLobbyData(activeLobbyId, "HostAddress");

			if (string.IsNullOrEmpty(hostAddress))
			{
				Debug.LogError("Host has not set custom HostAddress data");
				buttonContainer.gameObject.SetActive(true);
				return;
			}

			createLobbyButton.interactable = false;
			OnLobbyJoin?.Invoke();
		}

		private void HandleLobbyJoin()
		{
			lobbyBrowserContainer.SetActive(false);
		}
	}
}