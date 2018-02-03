using DSharpPlus.Entities;
using ImageCommands.Internals;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace ImageCommands.Utilities
{
    class MetaCommand : ModernDiscordCommand
    {
        public override string Name => "Meta";

        public override string Description => "Returns metadata for the given image";

        public override string[] Aliases => new[] { "meta" };

        public Task<CommandResult> RunImageCommand([Implicit] Image<Rgba32> image)
        {
            DiscordEmbedBuilder b = Context.GetEmbedBuilder("Image");
            b.AddField("Resolution", $"{image.Width}x{image.Height} ({image.Width * image.Height} pixels)", true);
            b.AddField("Frames", image.Frames.Count.ToString(),  true);

            if(image.MetaData.ExifProfile != null)
            {
                foreach (var exif in image.MetaData.ExifProfile?.Values)
                {
                    b.AddField(exif.Tag.ToString(), exif.Value.ToString(), true);
                }
            }
            
            if(image.MetaData.IccProfile != null)
            {
                b.AddField("Class", image.MetaData.IccProfile.Header.Class.ToString(), true);
                b.AddField("Creation Date", image.MetaData.IccProfile.Header.CreationDate.ToString(), true);
                b.AddField("Creator Signature", image.MetaData.IccProfile.Header.CreatorSignature, true);
                b.AddField("Device Manafacturer", image.MetaData.IccProfile.Header.DeviceManufacturer.ToString(), true);
                b.AddField("Device Model", image.MetaData.IccProfile.Header.DeviceModel.ToString(), true);
                b.AddField("Device Attributes", image.MetaData.IccProfile.Header.DeviceAttributes.ToString(), true);
            }

            foreach (var property in image.MetaData.Properties)
            {
                if (!string.IsNullOrEmpty(property.Name) && !string.IsNullOrEmpty(property.Value))
                {
                    b.AddField(property.Name, property.Value, true);
                }
            }

            using (Image<Rgba32> tempImage = image.Clone())
            {
                Rgba32[] byteData = new Rgba32[1];
                tempImage.Mutate(m => m.Resize(1, 1));
                tempImage.SavePixelData(byteData);

                Rgba32 c = byteData[0];
                b.WithColor(new DiscordColor(c.R, c.G, c.B));
            }

            return Task.FromResult(image.ToResult(Context, emb: b));
        }
    }
}
