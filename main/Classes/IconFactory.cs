﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace client.Classes
{
    public class IconFactory
    {

        #region constants

        /// Represents the max allowed width of an icon.
        public const int MaxIconWidth = 256;

        /// Represents the max allowed height of an icon.
        public const int MaxIconHeight = 256;

        private const ushort HeaderReserved = 0;
        private const ushort HeaderIconType = 1;
        private const byte HeaderLength = 6;

        private const byte EntryReserved = 0;
        private const byte EntryLength = 16;

        private const byte PngColorsInPalette = 0;
        private const ushort PngColorPlanes = 1;

        #endregion

        #region methods

        /// Saves the specified <see cref="Bitmap"/> objects as a single 
        /// icon into the output stream.

        /// <param name="images">The bitmaps to save as an icon.</param>
        /// <param name="stream">The output stream.</param>

        /// The expected input for the <paramref name="images"/> parameter are 
        /// portable network graphic files that have a <see cref="Image.PixelFormat"/> 
        /// of <see cref="PixelFormat.Format32bppArgb"/> and where the
        /// width is less than or equal to <see cref="IconFactory.MaxIconWidth"/> and the 
        /// height is less than or equal to <see cref="MaxIconHeight"/>.

        /// <exception cref="InvalidOperationException">
        /// Occurs if any of the input images do 
        /// not follow the required image format. See remarks for details.

        /// <exception cref="ArgumentNullException">
        /// Occurs if any of the arguments are null.

        public static void SavePngsAsIcon(IEnumerable<Bitmap> images, Stream stream)
        {
            if (images == null)
            {
                throw new ArgumentNullException("images");
            }

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            /*
            // validates the pngs
            IconFactory.ThrowForInvalidPngs(images);
            */

            var orderedImages = images.OrderBy(i => i.Width)
                                           .ThenBy(i => i.Height)
                                           .ToArray();

            using (var writer = new BinaryWriter(stream))
            {

                // write the header
                writer.Write(HeaderReserved);
                writer.Write(HeaderIconType);
                writer.Write((ushort)orderedImages.Length);

                // save the image buffers and offsets
                var buffers = new Dictionary<uint, byte[]>();

                // tracks the length of the buffers as the iterations occur
                // and adds that to the offset of the entries
                uint lengthSum = 0;
                var baseOffset = (uint)(HeaderLength +
                                        EntryLength * orderedImages.Length);

                for (var i = 0; i < orderedImages.Length; i++)
                {
                    var image = orderedImages[i];

                    // creates a byte array from an image
                    var buffer = CreateImageBuffer(image);

                    // calculates what the offset of this image will be
                    // in the stream
                    var offset = (baseOffset + lengthSum);

                    // writes the image entry
                    writer.Write(GetIconWidth(image));
                    writer.Write(GetIconHeight(image));
                    writer.Write(PngColorsInPalette);
                    writer.Write(EntryReserved);
                    writer.Write(PngColorPlanes);
                    writer.Write((ushort)Image.GetPixelFormatSize(image.PixelFormat));
                    writer.Write((uint)buffer.Length);
                    writer.Write(offset);

                    lengthSum += (uint)buffer.Length;

                    // adds the buffer to be written at the offset
                    buffers.Add(offset, buffer);
                }

                // writes the buffers for each image
                foreach (var kvp in buffers)
                {

                    // seeks to the specified offset required for the image buffer
                    writer.BaseStream.Seek(kvp.Key, SeekOrigin.Begin);

                    // writes the buffer
                    writer.Write(kvp.Value);
                }
            }

        }

        private static void ThrowForInvalidPngs(IEnumerable<Bitmap> images)
        {
            foreach (var image in images)
            {
                if (image.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    throw new InvalidOperationException
                        (string.Format("Required pixel format is PixelFormat.{0}.",
                                       PixelFormat.Format32bppArgb.ToString()));
                }

                if (image.RawFormat.Guid != ImageFormat.Png.Guid)
                {
                    throw new InvalidOperationException
                        ("Required image format is a portable network graphic (png).");
                }

                if (image.Width > MaxIconWidth ||
                    image.Height > MaxIconHeight)
                {
                    throw new InvalidOperationException
                        (string.Format("Dimensions must be less than or equal to {0}x{1}",
                                       MaxIconWidth,
                                       MaxIconHeight));
                }
            }
        }

        private static byte GetIconHeight(Bitmap image)
        {
            if (image.Height == MaxIconHeight)
            {
                return 0;
            }

            return (byte)image.Height;
        }

        private static byte GetIconWidth(Bitmap image)
        {
            if (image.Width == MaxIconWidth)
            {
                return 0;
            }

            return (byte)image.Width;
        }

        private static byte[] CreateImageBuffer(Bitmap image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);

                return stream.ToArray();
            }
        }

        #endregion

    }
}
