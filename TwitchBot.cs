using System;
using System.Text.RegularExpressions;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchOAuthListener
{
    public class TwitchBot
    {
        private readonly TwitchClient _client;
        private readonly string _channel;

        public bool IsConnected { get; private set; }

        public event Action OnDisconnected;
        public event Action<ChatMessage> OnJoinCommandReceived;
        public event Action<string> OnUserBanned;
        public event Action<string> OnUserTimedOut;

        public TwitchBot(string username, string accessToken)
        {
            var credentials = new ConnectionCredentials(username, accessToken);

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            _channel = username;

            var webSocketClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(webSocketClient);

            _client.Initialize(credentials, _channel);

            _client.OnConnected += (_, _) => IsConnected = true;
            _client.OnDisconnected += (_, _) =>
            {
                IsConnected = false;
                OnDisconnected?.Invoke();
            };

            _client.OnChatCommandReceived += HandleChatCommand;
            _client.OnUserBanned += (_, e) => OnUserBanned?.Invoke(e.UserBan.Username);
            _client.OnUserTimedout += (_, e) => OnUserTimedOut?.Invoke(e.UserTimeout.Username);

            _client.Connect();
        }

        private void HandleChatCommand(object sender, OnChatCommandReceivedArgs e)
        {
            if (Regex.IsMatch(e.Command.CommandText, @"join[:\s]*([a-zA-Z,]*)", RegexOptions.IgnoreCase))
            {
                OnJoinCommandReceived?.Invoke(e.Command.ChatMessage);
            }
        }

        public void SendMessage(string message)
        {
            if (_client.IsConnected)
                _client.SendMessage(_channel, message);
        }

        public void Disconnect()
        {
            if (_client.IsConnected)
                _client.Disconnect();
        }
    }
}