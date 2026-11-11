using Mirror.BouncyCastle.Tls;
using Steamworks;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Game
{
    public class SteamLobby : MonoBehaviour
    {
		private Steam steamManager;

		[Header("Chat")]
		[SerializeField] private GameObject lobbyContainer;
        [SerializeField] private TMP_Text chatText;
		[SerializeField] private TMP_InputField inputField;
		[SerializeField] private ScrollRect chatScroll;
        private List<string> messages = new List<string>();
		private Callback<LobbyChatMsg_t> m_LobbyChatMsg; // Chat message
		private Callback<LobbyChatUpdate_t> m_LobbyStatusUpdate; // Chat status update

		[Header("Members")]
		[SerializeField] private TMP_Text membersListText;
		private string lobbyName;

		private void Awake()
		{
			steamManager = FindAnyObjectByType<Steam>();
			if (steamManager == null) this.enabled = false;
			Steam.OnLobbyJoin += HandleLobbyJoin;
		}

		private void Start()
		{
			m_LobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
			m_LobbyStatusUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyStatusUpdate);
			inputField.onSubmit.AddListener(SendChatMessage);		
		}

		private void OnDestroy()
		{
			inputField.onSubmit.RemoveAllListeners();
			Steam.OnLobbyJoin -= HandleLobbyJoin;
		}

		public void SendChatMessage(string msg)
		{
			string message = inputField.text;
			if (string.IsNullOrEmpty(message)) return;
			if (steamManager.ActiveLobbyId == CSteamID.Nil || steamManager.ActiveLobbyId.m_SteamID == 0)
			{
				Debug.LogError("Not in lobby!");
				return;
			}

			// encode message
			byte[] data = Encoding.UTF8.GetBytes(message);

			// send message
			bool success = SteamMatchmaking.SendLobbyChatMsg(steamManager.ActiveLobbyId, data, data.Length);

			if (!success)
			{
				Debug.LogError("Send message fail!");
			}

			inputField.text = "";
			inputField.ActivateInputField();
		}

		public void OnLobbyChatMsg(LobbyChatMsg_t messageCallback)
        {
			// Make sure that the callback is meant for this lobby
			if (messageCallback.m_ulSteamIDLobby != steamManager.ActiveLobbyId.m_SteamID) return;

			// Allocate a buffer to hold the incoming data (Steam lobby messages are typically small text)
			byte[] data = new byte[4096];

			CSteamID senderID;
			EChatEntryType chatEntryType;

			// Fetch the message from Steam
			int bytesRead = SteamMatchmaking.GetLobbyChatEntry(
				new CSteamID(messageCallback.m_ulSteamIDLobby),
				(int)messageCallback.m_iChatID,
				out senderID,
				data,
				data.Length,
				out chatEntryType
			);

			// Verify it is a standard chat message
			if (chatEntryType == EChatEntryType.k_EChatEntryTypeChatMsg)
			{
				// Convert bytes back to string
				string message = Encoding.UTF8.GetString(data, 0, bytesRead);

				// Get the sender's Steam display name
				string senderName = SteamFriends.GetFriendPersonaName(senderID);
				if(senderName == SteamFriends.GetPersonaName())
				{
					senderName = $"<color=orange>{senderName}</color>";
				}

				//Debug.Log($"[Lobby Chat] {senderName}: {message}");
				messages.Add($"<b>{senderName}</b>: {message}");
				if (messages.Count > 100)
					messages.RemoveAt(0);
				UpdateChat();
			}
			
		}

		private void OnLobbyStatusUpdate(LobbyChatUpdate_t callback)
		{
			// Make sure that the callback is meant for this lobby
			if (callback.m_ulSteamIDLobby != steamManager.ActiveLobbyId.m_SteamID) return;

			// User who's state changed
			CSteamID changedUser = new CSteamID(callback.m_ulSteamIDUserChanged);

			// User who changed their state (if you want owners to be able to kick users, for example)
			CSteamID makingChangeUser = new CSteamID(callback.m_ulSteamIDMakingChange);
						
			string userName = SteamFriends.GetFriendPersonaName(changedUser);
			uint stateChangeFlags = callback.m_rgfChatMemberStateChange;

			if ((stateChangeFlags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
			{
				messages.Add($"<color=grey>{userName} joined the lobby!</color>");
				if (messages.Count > 100)
					messages.RemoveAt(0);
				UpdateChat();
				UpdateMemberStatus();
				
			}

			if ((stateChangeFlags & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
			{
				messages.Add($"<color=grey>{userName} left the lobby!</color>");
				if (messages.Count > 100)
					messages.RemoveAt(0);
				UpdateChat();
				UpdateMemberStatus();
			}
		}

		private void UpdateChat()
		{
			string s = "";
			for (int i = 0; i < messages.Count; i++)
			{
				if(i == messages.Count-1)
					s += "<size=+5>" + messages[i] + "</size>\n";
				else
					s += messages[i] + "\n";
			}
			chatText.text = s;
			Canvas.ForceUpdateCanvases();
			chatScroll.verticalNormalizedPosition = 0f;
		}

		private void HandleLobbyJoin()
		{
			lobbyContainer.SetActive(true);
			lobbyName = SteamMatchmaking.GetLobbyData(steamManager.ActiveLobbyId, "name");
			membersListText.text = lobbyName;
			UpdateMemberStatus();
		}

		private void UpdateMemberStatus()
		{
			int memberCount = SteamMatchmaking.GetNumLobbyMembers(steamManager.ActiveLobbyId);
			membersListText.text = $"<color=orange><size=+10>{lobbyName}</size></color>\n\n";
			for (int i = 0; i < memberCount; i++)
			{
				CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(steamManager.ActiveLobbyId, i);
				string memberName = SteamFriends.GetFriendPersonaName(memberID);
				membersListText.text += $"  {i + 1}. {memberName}\n";
			}
		}
	}
}