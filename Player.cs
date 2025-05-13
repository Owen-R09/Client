using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Client
{
    public class Player
    {
        public string name;
        public int xPos;
        public int yPos;

        public Player(string _name, int _xPos, int _yPos)
        {
            name = _name;
            xPos = _xPos;
            yPos = _yPos;
        }
    }
}
