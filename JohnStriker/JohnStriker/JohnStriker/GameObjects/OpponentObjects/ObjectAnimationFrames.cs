using System;
using DigitalRune.Mathematics.Algebra;


namespace JohnStriker.GameObjects.OpponentObjects
{
    public class ObjectAnimationFrames
    {
        public string Thrusters { get; private set; }
        public string Rotation { get; private set; }
        public string Pitch { get; private set; }
        public TimeSpan Time { get; private set; }

        public Vector3F ValPosition { get; private set; }
        public Vector3F ValRotation { get; private set; }

        public ObjectAnimationFrames(string thrusters, string rotation, string pitch, TimeSpan time)
        {
            if (thrusters == "thrusters forward")
            {
                ValPosition = new Vector3F(1, 1, 1);
            }
            if (rotation == "rotate left")
            {
                ValRotation = new Vector3F(2, 2, 2);
            }
            if (rotation == "rotate right")
            {
                ValRotation = new Vector3F(3, 3, 3);
            }
            //Thrusters = thrusters;
            //Rotation = rotation;
            Pitch = pitch;
            Time = time;
        }
    }


  
}
