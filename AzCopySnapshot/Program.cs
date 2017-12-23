using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace AzCopySnapshot
{
	class Program
	{
		static void Main(string[] args)
		{
			// read input file
			Input i = JsonConvert.DeserializeObject<Input>(File.ReadAllText("AzCopySnapshot.input.json"));
			
			// acquire auth token
			var token = GetAccessTokenAsync(i.ServicePrincipal).Result;

			//// get subscription info
			//string data = AzureApiGet("https://management.azure.com/subscriptions?api-version=2014-04-01", token);
			//Console.WriteLine(FormatJson(data) + "\n");

			// copy snapshot between subscriptions
			// from: https://docs.microsoft.com/en-us/rest/api/manageddisks/snapshots/snapshots-create-or-update
			var url = $"https://management.azure.com/subscriptions/{i.Destination.SubscriptionId}/resourceGroups/{i.Destination.ResourceGroup}/providers/Microsoft.Compute/snapshots/{i.SnapshotName}?api-version=2017-03-30";
			var body = "{ \"name\": \"" + i.SnapshotName + "\", \"location\": \"" + i.Source.Location + "\", \"properties\": { \"creationData\": {	\"createOption\": \"Copy\", \"sourceResourceId\": \"subscriptions/" + i.Source.SubscriptionId + "/resourceGroups/" + i.Source.ResourceGroup + "/providers/Microsoft.Compute/snapshots/" + i.SnapshotName + "\" }}}";

			string location = "";
			var result = AzureApiPut(url, body, token, ref location);
			Console.WriteLine(FormatJson(result));

			while (true)
			{
				if (string.IsNullOrWhiteSpace(location)) break;
				result = AzureApiGet(location, token);
				Console.WriteLine();
				Console.WriteLine(FormatJson(result));

				if (result.Contains("\"provisioningState\": \"Succeeded\"")) break;
				Thread.Sleep(10000);
			}

			Console.WriteLine("Copy is complete!");
			Console.ReadKey();
		}

		private static string AzureApiGet(string uriString, String token)
		{
			Uri uri = new Uri(String.Format(uriString));

			// Create the request
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
			httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Method = "GET";

			// Get the response
			HttpWebResponse httpResponse = null;
			try
			{
				httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error from " + uri + ": " + ex.Message);
				return null;
			}

			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				return streamReader.ReadToEnd();
			}
		}

		private static string AzureApiPut(string uriString, string body, String token, ref string location)
		{
			Uri uri = new Uri(String.Format(uriString));

			// Create the request
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
			httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Method = "PUT";

			try
			{
				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					streamWriter.Write(body);
					streamWriter.Flush();
					streamWriter.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error setting up stream writer: " + ex.Message);
			}

			// Get the response
			HttpWebResponse httpResponse = null;
			try
			{
				httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error from: " + uri + ": " + ex.Message);
				return null;
			}

			location = httpResponse.Headers["Location"];

			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				return streamReader.ReadToEnd();
			}
		}

		private static async Task<string> GetAccessTokenAsync(ServicePrincipal principal)
		{
			// powershell commands used to create the client credentials, see: https://goo.gl/NJIqZ1
			//$aadApp = New-AzureRmADApplication -DisplayName “Madras Console Apps” -HomePage “https://platform.digimarc.net” -IdentifierUris “https://platform.digimarc.net” -Password “D1g1Marc9405”
			//Write-Host "AppId: $aadApp.ApplicationId"
			//New-AzureRmADServicePrincipal -ApplicationId $aadApp.ApplicationId
			//New-AzureRmRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $aadApp.ApplicationId
			//$subscription = Get-AzureRmSubscription –SubscriptionName "Madras Dev"
			//$creds = Get-Credential
			//Login-AzureRmAccount -Credential $creds -ServicePrincipal -Tenant $subscription.TenantId

			var authenticationContext = new AuthenticationContext("https://login.windows.net/" + principal.ActiveDirectoryDomain);
			var credential = new ClientCredential(principal.ApplicationId, principal.ApplicationPassword);
			var result = await authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);

			if (result == null)
				throw new InvalidOperationException("Failed to obtain the JWT token");

			string token = result.AccessToken;
			return token;
		}

		private static string FormatJson(string json)
		{
			if (json is null) return string.Empty;

			dynamic parsedJson = JsonConvert.DeserializeObject(json);
			return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
		}
	}
}
