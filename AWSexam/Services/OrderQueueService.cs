using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSexam.Models;

namespace AWSexam.Services
{
    public class OrderQueueService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IAmazonSQS _sqs;
        private readonly string _queueUrl;

        public OrderQueueService(IAmazonSQS sqs, IConfiguration config)
        {
            _sqs = sqs;
            _queueUrl = config["Sqs:QueueUrl"]
                ?? throw new InvalidOperationException("Не задано налаштування Sqs:QueueUrl");
        }

        public async Task SendOrderCreatedAsync(Order order)
        {
            var message = new
            {
                OrderId = order.Id,
                order.CustomerName,
                order.CustomerEmail,
                order.CustomerPhone,
                order.Total,
                order.CreatedAt,
                Items = order.Items.Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product?.Name,
                    i.Quantity,
                    i.UnitPrice
                })
            };

            await _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(message, JsonOptions)
            });
        }
    }
}
