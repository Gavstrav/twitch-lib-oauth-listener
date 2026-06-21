using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TwitchLib.Api;

namespace TwitchOAuthListener
{
    public class TwitchAuthListener
    {
        private readonly string _clientId;
        private readonly string _redirectUri;
        private readonly string _authUrl;
		private const string OAUTH_HTML = @"
		<!DOCTYPE html>
		<html lang='en'>
		<head>
				<meta charset='UTF-8' />
				<title>Twitch OAuth</title>
		</head>
		<body>
				<h2>Authorization Successful</h2>
				<p>You can close this window.</p>

				<script>
						var accessToken = window.location.hash.split('&')[0].split('=')[1];

						var xhr = new XMLHttpRequest();
						xhr.open('POST', 'http://localhost:3000', true);
						xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');

						xhr.onreadystatechange = function () {
								if (xhr.readyState === 4 && xhr.status === 200) {
										window.close();
								}
						};

						xhr.send('access_token=' + accessToken);

						window.history.replaceState(null, null, window.location.pathname);
				</script>
		</body>
		</html>";

        private HttpListener _listener;

        public event Action<TwitchAPI, TwitchBot> OnConnected;

        public TwitchAuthListener(string clientId, string redirectUri = "http://localhost:3000")
        {
            _clientId = clientId;
            _redirectUri = redirectUri;

            _authUrl =
                $"https://id.twitch.tv/oauth2/authorize" +
                $"?response_type=token" +
                $"&client_id={clientId}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope=chat:read+chat:edit";
        }

        public void Start()
        {
            _listener?.Stop();

            _listener = new HttpListener();
            _listener.Prefixes.Add(_redirectUri + "/");
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();

                    if (context.Request.HttpMethod == "GET")
                        HandleGet(context);
                    else if (context.Request.HttpMethod == "POST")
                        HandlePost(context);
                }
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = _authUrl,
                UseShellExecute = true
            });
        }

        private void HandleGet(HttpListenerContext context)
        {
            var buffer = Encoding.UTF8.GetBytes(OAUTH_HTML);

            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private void HandlePost(HttpListenerContext context)
        {
            string body;

            using (var reader = new StreamReader(context.Request.InputStream))
                body = reader.ReadToEnd();

            var parameters = HttpUtility.ParseQueryString(body);
            var accessToken = parameters["access_token"];

            context.Response.StatusCode = 200;
            context.Response.Close();

            if (!string.IsNullOrEmpty(accessToken))
                _ = SetupTwitch(accessToken);
        }

        private async Task SetupTwitch(string accessToken)
        {
            var api = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _clientId,
                    AccessToken = accessToken
                }
            };

            var user = await api.Helix.Users.GetUsersAsync();

            if (user?.Users.Length > 0)
            {
                var login = user.Users[0].Login;

                var bot = new TwitchBot(login, accessToken);

                OnConnected?.Invoke(api, bot);

                _listener?.Stop();
            }
        }
    }
}