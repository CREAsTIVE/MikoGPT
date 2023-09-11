// Создание битовой карты
using static System.Net.Mime.MediaTypeNames;
using VkNet.Model;
using VkNet;
using VkNet.Model.Attachments;
using VkNet.Enums;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

public static class ImageUtils
{
    
    public static byte[] DrawText(string text, int scaleFactor=1)
    {
        FontCollection collection = new();
        FontFamily family = collection.Add("fonts/consolas.ttf");
        Font font = family.CreateFont(16, FontStyle.Regular);

        TextOptions options = new(font) { };
        string[] lines = text.Split('\n');
        float w = 0f;
        float h = 0f;
        for (var line = 0; line < lines.Length; line++)
        {
            var rect = TextMeasurer.Measure(lines[line], options);
            w = Math.Max(rect.Width, w);
            h += font.Size;
        }
        using (var image = new Image<Rgba32>((int)w, (int)h))
        {
            image.Mutate((im) =>
            {
                im.Clear(Color.Black);
                for (var line = 0; line < lines.Length; line++)
                {
                    options.Origin = new(0, font.Size*line);
                    im.DrawText(options, lines[line], Color.White);
                }
            });
            using (var imageStream = new MemoryStream())
            {
                image.Save(imageStream, new PngEncoder());
                return imageStream.ToArray();
            }
        }
    }
    public static Photo UploadImage(this VkApi api, byte[] image)
    {
        var server = api.Photo.GetMessagesUploadServer(0);
        HttpClient client = new HttpClient();
        MultipartFormDataContent content = new MultipartFormDataContent
        {
            { new StreamContent(new MemoryStream(image)), "file", "generated.png" }
        };
        var response = (client.PostAsync(server.UploadUrl, content).Result).Content.ReadAsStringAsync().Result;
        var res = api.Photo.SaveMessagesPhoto(response).ToArray();

        return res[0];
    }

}