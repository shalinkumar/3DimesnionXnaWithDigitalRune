using System;
using System.Collections.Generic;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Interpolation;
using Microsoft.Xna.Framework;

namespace JohnStriker.GameObjects.OpponentObjects
{
    public class KeyframedObjectAnimations
    {
        List<ObjectAnimationFrames> Frames = new List<ObjectAnimationFrames>();
        private bool Loop;
        private TimeSpan ElapsedTime = TimeSpan.FromSeconds(0);

        internal Vector3F Rotation { get; set; }
        internal Vector3F Position { get; set; }

        public KeyframedObjectAnimations(List<ObjectAnimationFrames> frames, bool loop)
        {
            Frames = frames;
            Loop = loop;
            Rotation = frames[0].ValPosition;
            Position = frames[0].ValRotation;
        }

        public void Update(TimeSpan elapsed)
        {
            //update the time
            ElapsedTime += elapsed;
            TimeSpan TotalTime = ElapsedTime;
            TimeSpan End = Frames[Frames.Count - 1].Time;

            //loop ariound the total time if necessary
            if (Loop)
            {
                while (TotalTime > End)
                    TotalTime -= End;
            }
            else // Otherwise, clamp to the end values
            {
                Position = Frames[Frames.Count - 1].ValPosition;
                Rotation = Frames[Frames.Count - 1].ValRotation;
                return;
            }

            int i = 0;

            //find the index of the current frame
            while (Frames[i + 1].Time < TotalTime)
            {
                i++;
            }

            // Find the time since the beginning of this frame
            TotalTime -= Frames[i].Time;

            // Find how far we are between the current and next frame (0 to 1)
            float amt = (float)((TotalTime.TotalSeconds) /
                (Frames[i + 1].Time - Frames[i].Time).TotalSeconds);

            // Interpolate position and rotation values between frames
            //Position = CatmullRom3D(
            //    Frames[Wrap(i - 1, Frames.Count - 1)].ValPosition,
            //    Frames[Wrap(i, Frames.Count - 1)].ValPosition,
            //    Frames[Wrap(i + 1, Frames.Count - 1)].ValPosition,
            //    Frames[Wrap(i + 2, Frames.Count - 1)].ValPosition,
            //    amt);
            Position = InterpolationHelper.Lerp(Frames[i].ValPosition, Frames[i + 1].ValPosition, amt);
            Rotation = InterpolationHelper.Lerp(Frames[i].ValRotation, Frames[i + 1].ValRotation, amt);
        }

        private Vector3F CatmullRom3D(Vector3F v1, Vector3F v2, Vector3F v3, Vector3F v4, float amt)
        {
            return new Vector3F(MathHelper.CatmullRom(v1.X, v2.X, v3.X, v4.X, amt), MathHelper.CatmullRom(v1.Y, v2.Y, v3.Y, v4.Y, amt), MathHelper.CatmullRom(v1.Z, v2.Z, v3.Z, v4.Z, amt));

        }

        // Wraps the "value" argument around [0, max]
        private int Wrap(int value, int max)
        {
            while (value > max)
            {
                value -= max;
            }
            while (value < 0)
            {
                value += max;
            }
            return value;
        }
    }
}
