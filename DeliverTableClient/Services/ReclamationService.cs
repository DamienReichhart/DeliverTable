using System.Text.Json;
using System.Text.Json.Nodes;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Interfaces;

namespace DeliverTableClient.Services;

public class ReclamationService(HttpClient httpClient) : IReclamationService
{
    public async Task<bool> CreateReclamation(CreateReclamationDto reclamation, List<Image> images)
    {
        using MultipartFormDataContent content = new();

        content.Add(new StringContent(reclamation.OrderId.ToString()), "OrderId");
        content.Add(new StringContent(reclamation.Description ?? ""), "Description");
        content.Add(new StringContent(reclamation.Type ?? ""), "Type");

        for (int i = 0; i < reclamation.Items.Count; i++)
        {
            content.Add(new StringContent(reclamation.Items[i].OrderItemId.ToString()), $"Items[{i}].OrderItemId");
            content.Add(new StringContent(reclamation.Items[i].HasImage.ToString()), $"Items[{i}].HasImage");
        }
        foreach (Image image in images)
        {
            content.Add(image.Content, image.Name, image.Name);
            Console.WriteLine(image.Name);
        }
        var response = await httpClient.PostAsync(ApiRoutes.Reclamation.Base, content);
        Console.WriteLine(response.StatusCode);
        return response.IsSuccessStatusCode;
    }
}