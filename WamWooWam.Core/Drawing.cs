#if NET35 || NET40 || NET45 || NET462 || NETSTANDARD2_0

using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;

#endif
using System;

namespace WamWooWam.Core
{
    public static class Drawing
    {

#if NET35 || NET40 || NET45 || NET462 || NETSTANDARD2_0
        public static Image ResizeImage(Image image, Size size, bool preserveAspectRatio = true)
        {
            int newWidth;
            int newHeight;

            if (preserveAspectRatio)
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;
                float percentWidth = (float)size.Width / (float)originalWidth;
                float percentHeight = (float)size.Height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = size.Width;
                newHeight = size.Height;
            }

            Image newImage = new Bitmap(newWidth, newHeight);
            using (var graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }
#endif

        public static void ScaleProportions(ref int currentWidth, ref int currentHeight, int maxWidth, int maxHeight)
        {
            if (currentWidth <= maxWidth && currentHeight <= maxHeight)
            {
                return;
            }
            else
            {
                double ratioX = (double)maxWidth / currentWidth;
                double ratioY = (double)maxHeight / currentHeight;
                double ratio = Math.Min(ratioX, ratioY);

                currentWidth = (int)(currentWidth * ratio);
                currentHeight = (int)(currentHeight * ratio);
            }
        }

        public static void Scale(ref int width, ref int height, int maxWidth, int maxHeight, StretchMode mode = StretchMode.Uniform)
        {
            if (mode == StretchMode.None)
                return;

            if (mode == StretchMode.Fill)
            {
                width = maxWidth;
                height = maxHeight;
                return;
            }

            double ratioX = (double)maxWidth / width;
            double ratioY = (double)maxHeight / height;
            double ratio = 0;

            if (mode == StretchMode.Uniform)
            {
                ratio = Math.Min(ratioX, ratioY);
            }

            if (mode == StretchMode.UniformToFill)
            {
                ratio = Math.Max(ratioX, ratioY);
            }

            width = (int)(width * ratio);
            height = (int)(height * ratio);
        }

        public enum StretchMode
        {
            None, Fill, Uniform, UniformToFill
        }
    }
}
