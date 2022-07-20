using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Lambda.SQSEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSServerless1
{
  public class Functions
  {

    public readonly string sqsName = "MAINQUEUE_NAME";

    public string mainProduct = "";
    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions()
    {
      mainProduct = Environment.GetEnvironmentVariable("MAIN_PRODUCT");
    }
    
    public void Consume(SQSEvent evnt, ILambdaContext context)
    {
      try
      {
        context.Logger.LogLine($"Consume Request / {mainProduct} \n");

        if (evnt.Records.Count > 1)
        {
          context.Logger.LogLine($"Consume Error: Only one msg can be handle at time \n");
          throw new InvalidOperationException("Only one msg can be handle at time");
        }


        var message = evnt.Records.FirstOrDefault();

        context.Logger.LogLine($"Consume Request: {message.Body} \n");
      }
      catch (Exception ex)
      {
        context.Logger.LogLine($"Consume Error: {ex.Message}\n");
        throw;
      }
    }

    public async Task<APIGatewayProxyResponse> Send(APIGatewayProxyRequest request, ILambdaContext context)
    {
      var input = request.QueryStringParameters["input"];
      context.Logger.LogLine($"Send Request: {input} \n");
      
      var client = new AmazonSQSClient(RegionEndpoint.USEast1);
      var nameQueue =  Environment.GetEnvironmentVariable(sqsName);

      context.Logger.LogLine($"Get SQS by queueName: {nameQueue} \n");

      var queueUrl = await GetQueueUrl(nameQueue, client, context);
      if (queueUrl != null)
      {
        var sqsrequest = new SendMessageRequest
        {
          QueueUrl = queueUrl,
          MessageBody = input
        };

        await client.SendMessageAsync(sqsrequest);

      }

      var response = new APIGatewayProxyResponse
      {
        StatusCode = (int)HttpStatusCode.OK,
        Body = input,
        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
      };

      return response;
    }

    public async Task<string> GetQueueUrl(string queueName, AmazonSQSClient client, ILambdaContext context)
    {
      if (client == null)
      {
        client = new AmazonSQSClient();
      }

      try
      {      
        var response = await client.GetQueueUrlAsync(queueName);

        if (response != null)
        {
          context.Logger.LogLine($"QueueUrl: {response.QueueUrl} \n");
          return response.QueueUrl;
        }
        else
        {
          context.Logger.LogLine($"QueueUrl: response nulo \n");
        }

        return null;
      }
      catch (Exception ex)
      {
        context.Logger.LogLine($"GetQueueUrl: erro ao tentar pegar a fila {queueName} - {ex.Message}\n");
        return null;
      }
    }
  }
}
