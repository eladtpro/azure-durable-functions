using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace ImageIngest.EventHub;
class Program
{
    static async Task Main(string[] args)
    {
        //IConfiguration config = new ConfigurationBuilder()
        //      .AddJsonFile("appsettings.json", true, true)
        //      .Build();

        await FindCustomerFiles();
        Console.Write("Press Enter to exit.");
        Console.ReadLine();
    }

    public static async Task<List<TaggedBlobItem>> FindCustomerFiles(string containerName = "images")
    {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsFTPStorage");
        BlobContainerClient blobContainerClient = new BlobContainerClient(connectionString, containerName);

        await foreach (BlobItem item in blobContainerClient.GetBlobsAsync(BlobTraits.All, BlobStates.None, "images/"))
            Console.WriteLine($"Found blob {item.Name}");

        var foundItems = new List<TaggedBlobItem>();
        string searchExpression = $"@container = '{containerName}' AND \"Status\"='Pending' AND \"Namespace\"= 'default'";
        await foreach (var page in blobContainerClient.FindBlobsByTagsAsync(searchExpression).AsPages())
        {
            foundItems.AddRange(page.Values);
        }
        return foundItems;
    }
}