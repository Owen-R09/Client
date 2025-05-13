using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections;
using System.Collections.Generic;
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

        private Texture2D playerTexture;

        public static NetPeerConfiguration config = new NetPeerConfiguration("Game_App");
        public NetClient client = new NetClient(config);

        public string playerName = "";
        public bool isTyping = true;
        public bool isValidName = false;

        public KeyboardState previousKeyBoard;

        public string IP;

        public int xPos = 5;
        public int yPos = 5;

        public int speed = 10;

        public List<Player> players = new List<Player>();

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
            playerTexture = Content.Load<Texture2D>("GibbonRef");

            previousKeyBoard = Keyboard.GetState();
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            var msg = client.CreateMessage();
            msg.Write("S3," + playerName);
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
                client.Connect(IP, 1989);
            }

            if (isTyping)
            {
                var msg = client.CreateMessage();
                msg.Write("S4," + playerName);
                client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);

                KeyboardState currentKeyBoard = Keyboard.GetState();

                foreach (Keys keys in currentKeyBoard.GetPressedKeys())
                {
                    if (previousKeyBoard.IsKeyUp(keys))
                    {
                        if (keys == Keys.Back && playerName.Length > 0)
                        {
                            playerName = playerName.Substring(0, playerName.Length - 1);
                        }
                        else if (keys == Keys.Enter && isValidName)
                        {
                            msg = client.CreateMessage();
                            msg.Write("S1," + playerName);
                            client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);

                            Player player = new Player(playerName,5,5);
                            players.Add(player);

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

            NetIncomingMessage inc;
            while ((inc = client.ReadMessage()) != null)
            {
                if (inc.MessageType == NetIncomingMessageType.Data)
                {
                    ReadMessage(inc.ReadString());
                }
            }

            if (!isTyping)
            {
                KeyboardState currentKeyboardState = Keyboard.GetState();

                foreach (Keys key in currentKeyboardState.GetPressedKeys())
                {
                    if(key == Keys.Left && xPos >= 5)
                    {
                        xPos -= 1 * speed;
                    }
                    else if(key == Keys.Right && xPos <= 800)
                    {
                        xPos += 1 * speed;
                    }
                    else if(key == Keys.Up && yPos >= 5)
                    {
                        yPos -= 1 * speed;
                    }
                    else if(key == Keys.Down && yPos <= 800)
                    {
                        yPos += 1 * speed;
                    }

                    var msg = client.CreateMessage();
                    msg.Write($"S2,{playerName}:{xPos}:{yPos}");
                    client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                }
            }

            base.Update(gameTime);
        }

        public void ReadMessage(string message)
        {
            string opcode;
            string operand;
            string[] split = message.Split(',');

            try
            {
                opcode = split[0];
                operand = split[1];
            }
            catch
            {
                return;
            }

            switch (opcode)
            {
                case "C1":
                    string newName = operand.Split(':')[0];
                    int xPos = int.Parse(operand.Split(':')[1]);
                    int yPos = int.Parse(operand.Split(':')[2]);

                    if (newName != playerName)
                    {
                        Player player = new Player(newName,xPos,yPos);
                        players.Add(player);
                    }

                    break;
                case "C2":
                    string name = operand.Split(":")[0];
                    int _xPos = int.Parse(operand.Split(':')[1]);
                    int _yPos = int.Parse(operand.Split(':')[2]);

                    foreach (Player player in players)
                    {
                        if(player.name == name)
                        {
                            player.xPos = _xPos;
                            player.yPos = _yPos;
                        }
                    }

                    break;
                case "C4":
                    isValidName = (operand == "True") ? true : false;

                    break;
            }
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
                    return ip.ToString();
            }

            throw new System.Exception("No Adapters found!");
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SpriteBatch spriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont font = Content.Load<SpriteFont>("myFont");

            spriteBatch.Begin();

            if (client.ServerConnection != null && isTyping == true)
            {
                spriteBatch.DrawString(font, "Connected to Server", new Vector2(5, 20), Color.Black);
                spriteBatch.DrawString(font, $"Enter your Name: {playerName}", new Vector2(5, 120), Color.Black);

                spriteBatch.DrawString(font, $"IP: {IP}", new Vector2(5, 70), Color.Black);
            }
            else if (client.ServerConnection == null && isTyping == true)
            {
                spriteBatch.DrawString(font, "Not Connected to Server", new Vector2(5, 20), Color.Black);
                spriteBatch.DrawString(font, $"IP: {IP}", new Vector2(5, 70), Color.Black);
            }

            if (!isTyping)
            {
                foreach (Player player in players)
                {
                    spriteBatch.Draw(playerTexture, new Vector2(player.xPos, player.yPos), Color.White);
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
