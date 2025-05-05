using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static NetPeerConfiguration config = new NetPeerConfiguration("Game_App");
        public NetClient client = new NetClient(config);

        public string playerName = "";
        public bool isTyping = true;

        public KeyboardState previousKeyBoard;

        public string IP;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            IP = GetPublicIP();
            client.Start();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            previousKeyBoard = Keyboard.GetState();
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            var msg = client.CreateMessage();
            msg.Write("M2," + playerName);
            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
            client.Disconnect("");

            base.OnExiting(sender, args);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if(client.ServerConnection == null && !string.IsNullOrEmpty(IP))
            {
                Thread.Sleep(1000);
                client.Connect("127.0.0.1", 1989);
            }

            if (isTyping)
            {
                KeyboardState currentKeyBoard = Keyboard.GetState();

                foreach (Keys keys in currentKeyBoard.GetPressedKeys())
                {
                    if (previousKeyBoard.IsKeyUp(keys))
                    {
                        if (keys == Keys.Back && playerName.Length > 0)
                        {
                            playerName = playerName.Substring(0, playerName.Length - 1);
                        }
                        else if (keys == Keys.Enter)
                        {
                            var msg = client.CreateMessage();
                            msg.Write("M1," + playerName);
                            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                            isTyping = false;
                        }
                        else
                        {
                            char c = ConvertToChar(keys, currentKeyBoard.IsKeyDown(Keys.LeftShift) || currentKeyBoard.IsKeyDown(Keys.RightShift));
                            if (c != '\0')
                            {
                                playerName += c;
                            }
                        }
                    }
                }

                previousKeyBoard = currentKeyBoard;
            }

            base.Update(gameTime);
        }

        public char ConvertToChar(Keys key, bool shift)
        {
            if(key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)key;
                return shift ? c : char.ToLower(c);
            }
            else if(key >= Keys.D0 && key <= Keys.D9)
            {
                string numbers = shift ? ")!@£$%^&*(" : "0123456789";
                return numbers[key - Keys.D0];
            }

            if(key == Keys.Space)
                return ' ';

            return '\0';
        }

        public string GetPublicIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString(); // e.g. 192.168.1.42
            }

            throw new System.Exception("No Adapters found!");
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont font = Content.Load<SpriteFont>("myFont");

            spriteBatch.Begin();

            spriteBatch.DrawString(font, $"IP: {IP}", new Vector2(5, 70), Color.Black);

            if (client.ServerConnection != null)
            {
                spriteBatch.DrawString(font, "Connected to Server", new Vector2(5, 20), Color.Black);
            }
            else
            {
                spriteBatch.DrawString(font, "Not Connected to Server", new Vector2(5, 20), Color.Black);
            }

            if(client.ServerConnection != null)
            {
                if (isTyping)
                    spriteBatch.DrawString(font, $"Enter your Name: {playerName}", new Vector2(5, 120), Color.Black);
                else
                    spriteBatch.DrawString(font, $"Name: {playerName}", new Vector2(5, 120), Color.Black);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
