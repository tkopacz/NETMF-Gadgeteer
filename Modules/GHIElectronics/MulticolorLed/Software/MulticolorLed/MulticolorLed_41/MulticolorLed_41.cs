﻿using System;

using GTM = Gadgeteer.Modules;

namespace Gadgeteer.Modules.GHIElectronics
{
    /// <summary>
    /// Represents a multi-color Light Emitting Diode (LED) module.
    /// </summary>
    /// <remarks>
    /// <note>
    ///  <see cref="MulticolorLed"/> derives from 
    ///  <see cref="Gadgeteer.Modules.Module.DaisyLinkModule"/>. This means that the hardware modules
    ///  represented by this class can be chained together on the same socket.
    /// </note>
    /// <para>
    /// The hardware module that this class represents consists of three light emitting diodes, red, green, and blue.
    /// The methods and properties of <see cref="MulticolorLed"/> enable you to set these colors in any combination.
    /// For instance, you can set the red diode to its maximum brightness, the green diode to half of its maximum brightness,
    /// and the blue diode to one quarter of its maximum brightness. The brightness of each color component is represented
    /// by the values zero (off) to 255 (maximum).
    /// </para>
    /// <note>
    /// Although the hardware module contains three light emitting diodes, the rest of this topic, and the other topics that are part of this class 
    /// refer to the hardware module as a whole. For instance, the phrase "The LED is off" means that all three of the light emitting diodes are off, 
    /// and the phrase "The LED is blinking" means that one or more of the light emitting didoes are blinking.
    /// </note>
    /// <para>
    ///  A <see cref=" MulticolorLed"/> can be in one of six states:</para>
    /// <list type="bullet">
    ///  <item>Off - The LED is off.</item>
    ///  <item>On - The LED displays one or more colors that remain constant.</item>
    ///  <item>Blink - The LED displays one or more colors and then (after a default or specified duration) turns off. </item>
    ///  <item>Blink (repeat) - The LED blinks repeatedly.</item>
    ///  <item>Fade - The LED displays one or more colors that gradually lose intensity until the LED is off.</item>
    ///  <item>Fade (repeat) - The LED repeatedly fades and lights.</item>
    /// </list>
    /// <para>
    ///  Some methods of the <see cref=" MulticolorLed"/> class cause the current state of the LED to change.
    ///  For instance, if the LED is blinking and you call the <see cref="TurnRed"/> method, the LED will stop
    ///  blinking and remain red. Similarly, if the LED is displaying a constant color and you call the
    ///  <see cref="FadeOnce(Color)"/> method, the LED will fade from the specfied color and then turn off.
    /// </para>
    /// <para>
    ///  Other methods do not cause the state of the LED to change. For instance, if the LED is blinking red, 
    ///  and you call the <see cref="AddBlue"/> method, the LED will continue to blink, now in red and blue.
    ///  Also, if you set the <see cref="Color"/> property directly, it does not affect the display state of the LED.
    /// </para>
    /// </remarks>
    public class MulticolorLed : GTM.Module.DaisyLinkModule
    {
        private enum Registers : byte
        {
            R = 0,
            G = 1,
            B = 2,
            Configuration = 3,
            ResetTimers = 4,
            Color1 = 5,
        }

        private enum Mode : byte
        {
            Off = 0,
            Constant = 1,
            BlinkOnce = 2,
            BlinkRepeatedly = 3,
            FadeOnce = 4,
            FadeRepeatedly = 5,
            BlinkOnceInt = 6,
            BlinkRepeatedlyInt = 7,
            FadeOnceInt = 8,
            FadeRepeatedlyInt = 9,
        }

        private const byte GHI_DAISYLINK_MANUFACTURER = 0x10;
        private const byte GHI_DAISYLINK_TYPE_MULTICOLORLED = 0x01;
        private const byte GHI_DAISYLINK_VERSION_MULTICOLORLED = 0x01;

        /// <summary>
        /// Accessor/Mutator for the property used to correct the blue/green channels
        /// </summary>
        public bool GreenBlueSwapped { get; set; }
        private readonly TimeSpan _oneSecond = new TimeSpan(0, 0, 1);

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The mainboard socket that has the multi-color LED plugged into it.</param>
        public MulticolorLed(int socketNumber)
            : base(socketNumber, GHI_DAISYLINK_MANUFACTURER, GHI_DAISYLINK_TYPE_MULTICOLORLED, GHI_DAISYLINK_VERSION_MULTICOLORLED, GHI_DAISYLINK_VERSION_MULTICOLORLED, 50, "MulticolorLed")
        {
            // Initialize the LED to default color and state
            TurnOff();

            DaisyLinkInterrupt += new DaisyLinkInterruptEventHandler(MulticolorLED_DaisyLinkInterrupt);
        }

        /// <summary>
        /// Creates an array of new <see cref="MulticolorLed"/> objects, one for each hardware module that is physically connected to the specified socket.
        /// </summary>
        /// <param name="socketNumber">The socket to get the objects for.</param>
        /// <returns>An array of <see cref="MulticolorLed"/> objects.</returns>
        /// <remarks>
        /// <para>
        ///  Use this method to retrieve an array of newly instantiated objects that correspond to the hardware modules that are physically connected to the specified socket.
        ///  When using this method, keep in mind the following points:
        /// </para>
        /// <list type="bullet">
        ///  <item>
        ///   This method creates new objects. If you have already created objects associated with socket <paramref name="socketNumber"/>, this method will fail.
        ///  </item>
        ///  <item>This method should only be called once. Subsequent calls to this method will fail.</item>
        /// </list>
        /// <para>
        ///  This method is useful when you don't know the number of modules that are connected to the specified socket, or when that number can vary.
        ///  By calling this method, you can obtain the proper number of object references, in the correct order. The object returned in the first index 
        ///  of the array (index 0) corresponds to the hardware module that is first on the chain (that is, closest to the main board), the second index 
        ///  of the array (index 1) corresponds to the hardware module that is second on the chain, and so on.
        /// </para>
        /// <para>
        ///  If you create objects associated with socket <paramref name="socketNumber"/> before calling this method, or call this method more than once, this method
        ///  will fail because module objects have already been assigned their positons on the chain.
        /// </para>
        /// </remarks>
        public static MulticolorLed[] GetAll(int socketNumber)
        {
            int chainLength;
            try
            {
                chainLength = GetLengthOfChain(socketNumber);
                if (chainLength == 0) return new MulticolorLed[0];
            }
            catch (Exception e)
            {
                // no nodes on chain? 
                throw new Socket.InvalidSocketException("Cannot initialize DaisyLink on socket " + socketNumber, e);
            }

            MulticolorLed[] ret = new MulticolorLed[chainLength];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new MulticolorLed(socketNumber);
            }
            return ret;
        }

        /// <summary>
        /// Changes the color of the LED to blue, stopping a blink or fade if one is in progress.
        /// </summary>
        public void TurnBlue()
        {
            SendCommand(Color.Blue, Mode.Constant);
        }

        /// <summary>
        ///  Changes the color of the LED to red, stopping a blink or fade if one is in progress.
        /// </summary>
        public void TurnRed()
        {
            SendCommand(Color.Red, Mode.Constant);
        }

        /// <summary>
        ///  Changes the color of the LED to green, stopping a blink or fade if one is in progress.
        /// </summary>
        public void TurnGreen()
        {
            SendCommand(Color.Green, Mode.Constant);
        }
        /// <summary>
        ///  Turns the LED off.
        /// </summary>
        public void TurnOff()
        {
            // we use mode.constant not mode.off since if we use SetXIntensity later that saves a few calls
            SendCommand(Color.Black, Mode.Constant);
        }

        /// <summary>
        ///  Changes the color of the LED to white, stopping a blink or fade if one is in progress.
        /// </summary>
        public void TurnWhite()
        {
            SendCommand(Color.White, Mode.Constant);
        }

        /// <summary>
        /// Changes the color of the LED to the specified color, stopping a blink or fade if one is in progress.
        /// </summary>
        /// <param name="color">The color to change the LED to.</param>              
        public void TurnColor(Color color)
        {
            Color newColor = color;
            SendCommand(newColor, Mode.Constant);
        }

        /// <summary>
        /// Sets the red component of the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <param name="intensity">The amount to set for the red intensity, 0 (no red) to 255 (full red).</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void SetRedIntensity(byte intensity)
        {
            Color currentColor = GetCurrentColor();
            currentColor.R = intensity;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Sets the red component of the current color (if there is a two-color animation, this affects the first color)
        /// </summary>
        /// <param name="intensity">The amount to set for the red intensity.</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// If <paramref name="intensity"/> is outside the range 0-255, it is constrained to that range (the valid range for color intensities),
        /// and no exception is raised.
        /// </remarks>
        public void SetRedIntensity(int intensity)
        {
            if (intensity < 0) intensity = 0;
            if (intensity > 255) intensity = 255;
            SetRedIntensity((byte)intensity);
        }

        /// <summary>
        /// Sets the green component of the current color (if there is a two-color animation, this affects the first color)
        /// </summary>
        /// <param name="intensity">The amount to set for the green intensity, 0 (no green) to 255 (full green).</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void SetGreenIntensity(byte intensity)
        {
            Color currentColor = GetCurrentColor();
            currentColor.G = intensity;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Sets the green component of the current color (if there is a two-color animation, this affects the first color)
        /// </summary>
        /// <param name="intensity">The amount to set for the green intensity.</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// If <paramref name="intensity"/> is outside the range 0-255, it is constrained to that range (the valid range for color intensities),
        /// and no exception is raised.
        /// </remarks>
        public void SetGreenIntensity(int intensity)
        {
            if (intensity < 0) intensity = 0;
            if (intensity > 255) intensity = 255;
            SetGreenIntensity((byte)intensity);
        }

        /// <summary>
        /// Sets the blue component of the current color (if there is a two-color animation, this affects the first color)
        /// </summary>
        /// <param name="intensity">The amount to set for the blue intensity, 0 (no blue) to 255 (full blue).</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void SetBlueIntensity(byte intensity)
        {
            Color currentColor = GetCurrentColor();
            currentColor.B = intensity;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Sets the blue component of the current color (if there is a two-color animation, this affects the first color)
        /// </summary>
        /// <param name="intensity">The amount to set for the blue intensity.</param>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// If <paramref name="intensity"/> is outside the range 0-255, it is constrained to that range (the valid range for color intensities),
        /// and no exception is raised.
        /// </remarks>
        public void SetBlueIntensity(int intensity)
        {
            if (intensity < 0) intensity = 0;
            if (intensity > 255) intensity = 255;
            SetBlueIntensity((byte)intensity);
        }


        /// <summary>
        /// Adds a full red component to the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void AddRed()
        {
            Color currentColor = GetCurrentColor();
            currentColor.R = 255;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Removes all of the red component from the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void RemoveRed()
        {
            Color currentColor = GetCurrentColor();
            currentColor.R = 0;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Adds a full green component to the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void AddGreen()
        {
            Color currentColor = GetCurrentColor();
            currentColor.G = 255;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Removes all of the green component from the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void RemoveGreen()
        {
            Color currentColor = GetCurrentColor();
            currentColor.G = 0;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Adds a full blue component to the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void AddBlue()
        {
            Color currentColor = GetCurrentColor();
            currentColor.B = 255;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Removes all of the blue component from the current color (if there is a two-color animation, this affects the first color).
        /// </summary>
        /// <remarks>
        /// When you call this method, the display state of the <see cref="MulticolorLed"/> does not change unless it is off. 
        /// </remarks>
        public void RemoveBlue()
        {
            Color currentColor = GetCurrentColor();
            currentColor.B = 0;
            SendCommand(currentColor);
        }

        /// <summary>
        /// Returns the current LED color.
        /// </summary>
        /// <returns>The current <see cref="Color"/>.</returns>
        /// <remarks>
        ///  If you call this method while the <see cref="MulticolorLed"/> is performing an animation (blink or fade), 
        ///  the return value represents the first state of the animation.
        /// </remarks>
        public Color GetCurrentColor()
        {
            // NOTE: THE COLOR RETURNED IS THE COLOR YOU WANT IT TO BE. THIS IS NOT EFFECTED BY THE COLOR SWAP.
            byte c1 = Read((byte)(DaisyLinkOffset + Registers.Color1));
            byte c2 = Read((byte)(DaisyLinkOffset + Registers.Color1 + 1));
            byte c3 = Read((byte)(DaisyLinkOffset + Registers.Color1 + 2));

            return GreenBlueSwapped ? new Color(c1, c3, c2) : new Color(c1, c2, c3);
        }

        /// <summary>
        /// Causes the LED to light in the specified color for one second, and then turn off.
        /// </summary>
        /// <param name="color">The color to display.</param>
        /// <remarks>
        /// The default blink time for this method is one second.
        /// </remarks>
        public void BlinkOnce(Color color)
        {
            SendCommand(color, _oneSecond, Color.Black, TimeSpan.Zero, Mode.BlinkOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color for the specified duration, and then turn off.
        /// </summary>
        /// <param name="color">The color to display.</param>
        /// <param name="blinkTime">The duration before the LED turns off.</param>
        public void BlinkOnce(Color color, TimeSpan blinkTime)
        {
            SendCommand(color, blinkTime, Color.Black, TimeSpan.Zero, Mode.BlinkOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color for the specified duration, and then switch to another color.
        /// </summary>
        /// <param name="blinkColor">The color to display until <paramref name="blinkTime"/> elapses.</param>
        /// <param name="blinkTime">The duration before the LED changes from <paramref name="blinkColor"/> to <paramref name="endColor"/>.</param>
        /// <param name="endColor">The color to switch to when <paramref name="blinkTime"/> elapses.</param>
        public void BlinkOnce(Color blinkColor, TimeSpan blinkTime, Color endColor)
        {
            SendCommand(blinkColor, blinkTime, endColor, TimeSpan.Zero, Mode.BlinkOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color for one second, turn off, and repeat.
        /// </summary>
        /// <param name="color">The color to display.</param>
        public void BlinkRepeatedly(Color color)
        {
            SendCommand(color, _oneSecond, Color.Black, _oneSecond, Mode.BlinkRepeatedly);
        }

        /// <summary>
        ///  Causes the LED to light in the specified color for the specified duration, 
        ///  switch to the second color for another specified duration, and repeat.
        /// </summary>
        /// <param name="color1">The color used for the first part of the blink.</param>
        /// <param name="blinkTime1">The duration before the LED changes from <paramref name="color1"/> to <paramref name="color2"/>.</param>
        /// <param name="color2">The color used for the second part of the blink.</param>
        /// <param name="blinkTime2">The duration before the LED changes from <paramref name="color2"/> back to <paramref name="color1"/>.</param>
        public void BlinkRepeatedly(Color color1, TimeSpan blinkTime1, Color color2, TimeSpan blinkTime2)
        {
            SendCommand(color1, blinkTime1, color2, blinkTime2, Mode.BlinkRepeatedly);
        }

        /// <summary>
        /// Causes the LED to light in the specified color, and then fade to black (off) in one second.
        /// </summary>
        /// <param name="color">The color to begin the fade with.</param>
        /// <remarks>
        /// The default fade time for this method is one second.
        /// </remarks>
        public void FadeOnce(Color color)
        {
            SendCommand(color, _oneSecond, Color.Black, TimeSpan.Zero, Mode.FadeOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color, and then fade to black (off) in the specified duration.
        /// </summary>
        /// <param name="color">The color to begin the fade with.</param>
        /// <param name="fadeTime">The duration of the fade.</param>
        public void FadeOnce(Color color, TimeSpan fadeTime)
        {
            SendCommand(color, fadeTime, Color.Black, TimeSpan.Zero, Mode.FadeOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color, and then fade to another color in the specified duration.
        /// </summary>
        /// <param name="fromColor">The color to begin the fade with.</param>
        /// <param name="fadeTime">The duration of the fade.</param>
        /// <param name="toColor">The color to end the fade with.</param>
        public void FadeOnce(Color fromColor, TimeSpan fadeTime, Color toColor)
        {
            SendCommand(fromColor, fadeTime, toColor, TimeSpan.Zero, Mode.FadeOnceInt);
        }

        /// <summary>
        /// Causes the LED to light in the specified color, fade to black (off) in one second, and repeat.
        /// </summary>
        /// <param name="color">The color to begin the fade with.</param>
        public void FadeRepeatedly(Color color)
        {
            SendCommand(color, _oneSecond, Color.Black, _oneSecond, Mode.FadeRepeatedly);
        }

        /// <summary>
        /// Cause the LED to repeatedly fade back and forth between two colors
        /// </summary>
        /// <param name="color1">The color to begin the fade with.</param>
        /// <param name="fadeTime1">The duration of the fade from <paramref name="color1"/> to <paramref name="color2"/>.</param>
        /// <param name="color2">The color of the second part of the fade.</param>
        /// <param name="fadeTime2">The duration of the fade from <paramref name="color2"/> to <paramref name="color1"/>.</param>
        public void FadeRepeatedly(Color color1, TimeSpan fadeTime1, Color color2, TimeSpan fadeTime2)
        {
            SendCommand(color1, fadeTime1, color2, fadeTime2, Mode.FadeRepeatedly);
        }

        // Fully changes the mode, timespans and colors
        private void SendCommand(Color color1, TimeSpan blinkTime1, Color color2, TimeSpan blinkTime2, Mode mode)
        {
            long time1 = blinkTime1.Ticks / 1000;
            long time2 = blinkTime2.Ticks / 1000;

            // send the parameters with mode off to avoid side effects of previous mode
            if (GreenBlueSwapped)
            {
                WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Off, 0x00,
                            color1.R, color1.B, color1.G,
                            color2.R, color2.B, color2.G,
                            (byte)(time1 >> 0), (byte)(time1 >> 8), (byte)(time1 >> 16), (byte)(time1 >> 24),
                            (byte)(time2 >> 0), (byte)(time2 >> 8), (byte)(time2 >> 16), (byte)(time2 >> 24));
            }
            else
            {
                WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Off, 0x00,
                            color1.R, color1.G, color1.B,
                            color2.R, color2.G, color2.B,
                            (byte)(time1 >> 0), (byte)(time1 >> 8), (byte)(time1 >> 16), (byte)(time1 >> 24),
                            (byte)(time2 >> 0), (byte)(time2 >> 8), (byte)(time2 >> 16), (byte)(time2 >> 24));
            }
            // now activate the correct mode
            WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)mode, 0x1);
        }


        // Writes color1 and changes the mode
        private void SendCommand(Color color, Mode mode)
        {
            // send the parameters with mode off to avoid side effects of previous mode
            if (GreenBlueSwapped)
            {
                WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Off, 0x0, color.R, color.B, color.G);
            }
            else
            {
                WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Off, 0x0, color.R, color.G, color.B);
            }
            // now activate the correct mode
            WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)mode, 0x1);
        }


        // Writes color1 without changing the mode, unless the mode is off in which case it becomes constant
        private void SendCommand(Color color)
        {
            Mode currentMode = (Mode)Read((byte)(DaisyLinkOffset + Registers.Configuration));
            if (currentMode == Mode.Off)
            {
                if (GreenBlueSwapped)
                {
                    WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Constant, 0x1, color.R,
                                color.B, color.G);
                }
                else
                {
                    WriteParams((byte)(DaisyLinkOffset + Registers.Configuration), (byte)Mode.Constant, 0x1, color.R,
                                color.G, color.B);
                }
            }
            else
            {
                if (GreenBlueSwapped)
                {
                    WriteParams((byte)(DaisyLinkOffset + Registers.Color1), color.R, color.B, color.G);
                }
                else
                {
                    WriteParams((byte)(DaisyLinkOffset + Registers.Color1), color.R, color.G, color.B);
                }
            }
        }

        void MulticolorLED_DaisyLinkInterrupt(DaisyLinkModule sender)
        {
            OnAnimationFinishedEvent(this);
        }


        /// <summary>
        /// The delegate that is used for the <see cref="AnimationFinished"/> event.
        /// </summary>
        /// <param name="led">The LED which finished its BlinkOnce or FadeOnce animation.</param>
        public delegate void AnimationFinishedEventHandler(MulticolorLed led);

        /// <summary>
        /// Raised when the LED has finished a BlinkOnce or FadeOnce animation.
        /// </summary>
        public event AnimationFinishedEventHandler AnimationFinished;
        private AnimationFinishedEventHandler onAnimationFinished;

        /// <summary>
        /// Raises the <see cref="AnimationFinished"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="MulticolorLed"/> that raised the event.</param>
        protected virtual void OnAnimationFinishedEvent(MulticolorLed sender)
        {
            if (onAnimationFinished == null) onAnimationFinished = new AnimationFinishedEventHandler(OnAnimationFinishedEvent);
            if (Program.CheckAndInvoke(AnimationFinished, onAnimationFinished, sender))
            {
                AnimationFinished(sender);
            }
        }
    }
}