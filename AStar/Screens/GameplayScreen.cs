#region Using Statements
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace AIProject
{

    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        SpriteFont gameFont;

        Vector2 playerPosition = new Vector2(100, 100);
        Vector2 enemyPosition = new Vector2(100, 100);

        Random random = new Random();

        float pauseAlpha;

        #endregion

        #region Initialization
        
        public class Node
        {
            public Vector2 position;
            public bool isuseable;
            public bool isactive;
            public bool isstart;
            public bool isgoal;
            public Vector2 gridlocation;
            public Vector2 parent;
            public Node(Vector2 pos,bool usa, Vector2 grid)
            {
                gridlocation = grid;
                position = pos;
                isuseable = usa;
                isactive = false;
                isstart = false;
                isgoal = false;
                parent = new Vector2(0,0);
            }
        }

       public Node[,] nodes = new Node[ 20,20];
       Vector2 current_node = new Vector2(1, 1);
       Vector2 start_node = new Vector2(1,1);
       Vector2 goal_node = Vector2.Zero;
       Vector2 debug = Vector2.Zero;
       bool runsearch = false;
       bool allowsearch = true;
       bool unreachable = false;
       bool goal_found = false;
       List<Node> openlist = new List<Node>();
       List<Node> closedlist = new List<Node>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            gameFont = content.Load<SpriteFont>("gamefont");
            
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 14; j++)
                {
                    nodes[i, j] = new Node(new Vector2((i + 2) * 25, (j + 2) * 25), true,new Vector2(i,j));
                    if (i==0 || i==14 || j==0 || j==13)
                        nodes[i, j].isuseable = false;
                }
            }
            Thread.Sleep(200);
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
                pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
            else
                pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

            if (IsActive)
            {
                //This is where the A* search begins
                if (runsearch && allowsearch)
                {
                    openlist.Add(nodes[(int)start_node.X, (int)start_node.Y]);
                    Vector2 lowestf;
                    goal_found = false;
                    unreachable = false;
                    while (!goal_found)
                    {
                        lowestf = openlist[0].gridlocation;
                        //Retrieves the node with the lowest F cost 
                        foreach (Node node in openlist)
                        {
                            if (calcf(node.gridlocation) < calcf(lowestf))
                                lowestf = node.gridlocation;
                        }
                        //adds said node to closed list(visit)
                        closedlist.Add(nodes[(int)lowestf.X, (int)lowestf.Y]);
                        openlist.Remove(nodes[(int)lowestf.X, (int)lowestf.Y]);
                        neighbor(new Vector2((int)lowestf.X, (int)lowestf.Y));
                        if (nodes[(int)lowestf.X, (int)lowestf.Y].isgoal == true)
                            goal_found = true;
                        if (openlist.Count == 0)
                        {
                            unreachable = true;
                            break;
                        }
                    }
                    if(!unreachable)
                    getpath(nodes[(int)goal_node.X, (int)goal_node.Y]);
                    runsearch = false;
                    allowsearch = false;
                }
            }
        }
        //populates the openlist(gets neighbors of the current node)
        public void neighbor(Vector2 current)
        {
            if (current.X >= 1 && current.Y >= 1 && current.X < 15 && current.Y < 14)
            {
                for (int i = (int)(current.X) - 1; i <= (int)current.X + 1; i++)
                {

                    for (int j = (int)(current.Y) - 1; j <= (int)current.Y + 1; j++)
                    {
                        if (nodes[i, j].isuseable && !closedlist.Contains(nodes[i, j]) && !openlist.Contains(nodes[i, j]))
                        {
                        openlist.Add(nodes[i, j]);
                            nodes[i,j].parent=current;
                        }
                        else if (openlist.Contains(nodes[i, j]))
                       {
                            if (calcg(nodes[(int)current.X,(int)current.Y].parent,new Vector2(i,j)) <=1)
                           {
                               nodes[i, j].parent = nodes[(int)current.X, (int)current.Y].parent;
                            }
                        }
                    }
                    
                }
            }
        }
        //calcs a new g for neighbors
        public int calcg(Vector2 start, Vector2 stop)
        {
            int y = (int)MathHelper.Distance((float)start.Y, (float)stop.Y);
            int x = (int)MathHelper.Distance((float)start.X, (float)stop.X);
            if (y > x)
                return y;
            else
                return x;
        }
        //calculates F
        public int calcf(Vector2 index)
        {
            int g = closedlist.Count;
            int h = (int)MathHelper.Distance(index.X, goal_node.X) + (int)MathHelper.Distance(index.Y, goal_node.Y);
            return (g + h);
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];
            KeyboardState prevkeyboardState = input.LastKeyboardStates[playerIndex];
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                Vector2 movement = Vector2.Zero;
                if (!runsearch)
                {
                    if (keyboardState.IsKeyDown(Keys.Left) && prevkeyboardState.IsKeyDown(Keys.Left) == false)
                        movement.X--;

                    if (keyboardState.IsKeyDown(Keys.Right) && prevkeyboardState.IsKeyDown(Keys.Right) == false)
                        movement.X++;

                    if (keyboardState.IsKeyDown(Keys.Up) && prevkeyboardState.IsKeyDown(Keys.Up) == false)
                        movement.Y--;

                    if (keyboardState.IsKeyDown(Keys.Down) && prevkeyboardState.IsKeyDown(Keys.Down) == false)
                        movement.Y++;

                    if (keyboardState.IsKeyDown(Keys.Enter) && prevkeyboardState.IsKeyDown(Keys.Enter) == false)
                    {
                        bool startexist =false;
                        bool goalexist=false;
                            foreach (Node node in nodes)
                            {
                                if (node!=null && node.isstart) startexist = true;
                                if (node!=null && node.isgoal) goalexist = true;
                            }
                            if(startexist && goalexist && allowsearch)
                            runsearch = true;
                    }

                    if (keyboardState.IsKeyDown(Keys.R) && prevkeyboardState.IsKeyDown(Keys.R) == false)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            for (int j = 0; j < 14; j++)
                            {
                                nodes[i, j].isuseable = true;
                                nodes[i, j].isgoal = false;
                                nodes[i, j].isstart = false;
                                nodes[i, j].parent = new Vector2(0,0);
                                if (i == 0 || i == 14 || j == 0 || j == 13)
                                    nodes[i, j].isuseable = false;
                            }
                        }
                        goal_found = false;
                        solution = new List<Node>();
                        openlist=new List<Node>();
                        closedlist=new List<Node>();
                        allowsearch = true;
                    }

                    if (keyboardState.IsKeyDown(Keys.S) && prevkeyboardState.IsKeyDown(Keys.S) == false)
                    {
                        foreach (Node node in nodes)
                        {
                         if (node!=null)
                            node.isstart = false;
                        }
                            nodes[(int)current_node.X, (int)current_node.Y].isstart = true;
                        start_node = current_node;
                    }
                    if (keyboardState.IsKeyDown(Keys.G) && prevkeyboardState.IsKeyDown(Keys.G) == false)
                    {
                        foreach (Node node in nodes)
                        {
                            if (node != null)
                                node.isgoal = false;
                        }
                        goal_node = current_node;
                        nodes[(int)current_node.X, (int)current_node.Y].isgoal = true;
                    }
                    if (keyboardState.IsKeyDown(Keys.C) && prevkeyboardState.IsKeyDown(Keys.C) == false)
                    {
                        nodes[(int)current_node.X, (int)current_node.Y].isuseable = true;
                        nodes[(int)current_node.X, (int)current_node.Y].isgoal = false;
                        nodes[(int)current_node.X, (int)current_node.Y].isstart = false;
                    }
                    
                    if (keyboardState.IsKeyDown(Keys.B) && prevkeyboardState.IsKeyDown(Keys.B) == false)
                    {
                        nodes[(int)current_node.X, (int)current_node.Y].isuseable = false;
                    }
                    Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                    movement.X += thumbstick.X;
                    movement.Y -= thumbstick.Y;

                    if (movement.Length() > 1)
                        movement.Normalize();

                    nodes[(int)current_node.X, (int)current_node.Y].isactive = false;

                    if ((current_node + movement).X < 14 && (current_node + movement).Y < 13)
                    {
                        if ((current_node + movement).X > 0 && (current_node + movement).Y > 0)
                            current_node += movement;
                        else movement = Vector2.Zero;

                    }
                    else movement = Vector2.Zero;

                    MathHelper.Clamp(current_node.X, 0, 14);
                    MathHelper.Clamp(current_node.Y, 0, 14);
                    nodes[(int)current_node.X, (int)current_node.Y].isactive = true;
                }
            }
        }
        Vector2 infopostition = new Vector2(430, 50);
        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // bg color
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            //texture used for the rectangles
            Texture2D pixel = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Begin();

            
           spriteBatch.DrawString(gameFont, "Controls;", infopostition, Color.White);
           spriteBatch.DrawString(gameFont, "Arrow keys to move", new Vector2 (infopostition.X,infopostition.Y+15), Color.Yellow);
           spriteBatch.DrawString(gameFont, "S to select start node", new Vector2(infopostition.X, infopostition.Y + 30), Color.Yellow);
           spriteBatch.DrawString(gameFont, "G to select goal node", new Vector2(infopostition.X, infopostition.Y + 45), Color.Yellow);
           spriteBatch.DrawString(gameFont, "B to select barrier node", new Vector2(infopostition.X, infopostition.Y + 60), Color.Yellow);
           spriteBatch.DrawString(gameFont, "C to clear the node to normal state", new Vector2(infopostition.X, infopostition.Y + 75), Color.Yellow);
           spriteBatch.DrawString(gameFont, "R to Reset all nodes to original state", new Vector2(infopostition.X, infopostition.Y + 90), Color.Yellow);
           spriteBatch.DrawString(gameFont, "ENTER to start search", new Vector2(infopostition.X, infopostition.Y + 105), Color.Yellow);
           spriteBatch.DrawString(gameFont, "Color Key:", new Vector2(infopostition.X, infopostition.Y + 185), Color.White);
           spriteBatch.DrawString(gameFont, "Useable node", new Vector2(infopostition.X, infopostition.Y + 200), Color.DarkGreen);
           spriteBatch.DrawString(gameFont, "Barrier node", new Vector2(infopostition.X, infopostition.Y + 215), Color.Indigo);
           spriteBatch.DrawString(gameFont, "Cursor", new Vector2(infopostition.X, infopostition.Y + 230), Color.Red);
           spriteBatch.DrawString(gameFont, "Start node", new Vector2(infopostition.X, infopostition.Y + 245), Color.Orange);
           spriteBatch.DrawString(gameFont, "Goal node", new Vector2(infopostition.X, infopostition.Y + 260), Color.Yellow);
           spriteBatch.DrawString(gameFont, "node viewed during search", new Vector2(infopostition.X, infopostition.Y + 275), Color.DarkMagenta);
           spriteBatch.DrawString(gameFont, "node visited during search", new Vector2(infopostition.X, infopostition.Y + 290), Color.Blue);
           spriteBatch.DrawString(gameFont, "node on the resulting best path", new Vector2(infopostition.X, infopostition.Y + 305), Color.Cyan);

            foreach (Node node1 in nodes)
            {
                if(node1 != null && node1.isgoal)
                    spriteBatch.Draw(pixel, new Rectangle((int)node1.position.X, (int)node1.position.Y, 15, 15), Color.Yellow);
                else
                        if (node1 != null && node1.isstart)
                    {
                        spriteBatch.Draw(pixel, new Rectangle((int)node1.position.X, (int)node1.position.Y, 15, 15), Color.Orange);
                    }
                    else
                            if (node1 != null && !node1.isuseable)
                        {
                            spriteBatch.Draw(pixel, new Rectangle((int)node1.position.X, (int)node1.position.Y, 20, 20), Color.Indigo);
                        }
                else if(node1 != null)
                spriteBatch.Draw(pixel, new Rectangle((int)node1.position.X,(int)node1.position.Y, 15, 15), Color.DarkGreen);
            }
            
            if (openlist.Count > 0)
            {
                foreach (Node node in openlist)
                    spriteBatch.Draw(pixel, new Rectangle((int)node.position.X, (int)node.position.Y, 15, 15), Color.DarkMagenta);
            }
            if (closedlist.Count > 0)
            {
                foreach (Node node in closedlist)
                    spriteBatch.Draw(pixel, new Rectangle((int)node.position.X, (int)node.position.Y, 15, 15), Color.Blue);
            }
           
            if (goal_found)
            {
                
                foreach (Node node in solution)
                    spriteBatch.Draw(pixel, new Rectangle((int)node.position.X, (int)node.position.Y, 15, 15), Color.Cyan);
            }

            foreach (Node node1 in nodes)
            {
                if (node1 != null && node1.isactive)
                {
                    spriteBatch.Draw(pixel, new Rectangle((int)node1.position.X, (int)node1.position.Y, 15, 15), Color.Red);
                }
            }
            if(unreachable)
                spriteBatch.DrawString(gameFont,"GOAL UNREACHABLE",new Vector2(15,15), Color.White);
            spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }
        }
       List<Node> solution = new List<Node>();

        //recursive fuction used to obtain the path from the goal to the start, using the parents that were assigned
        public void getpath(Node node)
        {
            solution.Add(node);
            if (node.isstart == false)
                getpath(nodes[(int)node.parent.X, (int)node.parent.Y]);
            
        }

        #endregion
    }
}
