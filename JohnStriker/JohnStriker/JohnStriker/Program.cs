using System;
using System.Diagnostics;
//using System.Windows.Forms;
using JohnStriker.Sample_Framework;
//using Microsoft.Xna.Framework.GamerServices;


namespace JohnStriker
{
#if WINDOWS || XBOX
    static class Program
    {
        private static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                // The debugger is attached:
                // Run the XNA game and let the debugger handle any exception messages.
                using (var game = new Game1())
                    game.Run();
            }
            else
            {
                // The debugger is NOT attached:
                // Run the XNA game and use a MessageBox to display any exception messages.
                try
                {
                    using (var game = new Game1())
                        game.Run();
                }
                catch (Exception exception)
                {
                    string message = SampleHelper.GetExceptionMessage(exception);
                    //MessageBox.Show(message, "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
#endif
}

