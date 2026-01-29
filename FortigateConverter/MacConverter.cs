using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FortigateConverter
{
    public class MacConverter
    {
        // Logger to write to Azure Console
        private readonly ILogger<MacConverter> _logger;
        public MacConverter(ILogger<MacConverter> logger)
        {
            _logger = logger;
        }

        // [Function("...")] is the name Azure uses to identify this function.
        [Function("ConvertMacAddress")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // STEP 1: READ THE RAW REQUEST
            // req.Body is still a stream, so this works the same!
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // STEP 2: DESERIALIZE (Convert JSON string to your C# Object)
            // TODO: Use JsonConvert.DeserializeObject...
            // Create options to ignore case
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var data = JsonSerializer.Deserialize<FortigateGenerationRequest>(requestBody, options);
            // Test if deserialization worked
            _logger.LogInformation($"Deserialized MacAddressList: {data?.MacAddressList}");

            // STEP 3: VALIDATE INPUT
            // TODO: Check if data.MacAddressList is empty.
            if (data is null || string.IsNullOrWhiteSpace(data.MacAddressList))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("MacAddressList is empty.");
                return errorResponse;
            }


            // STEP 4: LOGIC REGEX
            StringBuilder macListBuilder = new StringBuilder();
            List<string> cleanMacList = new List<string>();

            // Regex pattern for validating MAC addresses
            string pattern = @"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            MatchCollection matches = regex.Matches(data.MacAddressList);
            if (matches.Count == 0)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("No valid MAC addresses found.");
                return errorResponse;
            }
            _logger.LogInformation($"Found {matches.Count} valid MAC addresses.");

            foreach (Match match in matches)
            {
                // Replace hyphens with colons and convert to uppercase
                if (match.Value.Contains("-"))
                {
                    string formattedMac = match.Value.Replace("-", ":").ToUpper(CultureInfo.InvariantCulture);
                    // Add to clean list for UI
                    cleanMacList.Add(formattedMac);
                    macListBuilder.Append($"\"{formattedMac}\" ");
                    _logger.LogInformation($"Formatted MAC address: {formattedMac}");
                }
                else
                {
                    // Add to clean list for UI
                    cleanMacList.Add(match.Value.ToUpper(CultureInfo.InvariantCulture));
                    macListBuilder.Append($"\"{match.Value.ToUpper(CultureInfo.InvariantCulture)}\" ");
                    _logger.LogInformation($"Found valid MAC address: {match.Value}");
                }
            }

            string macListString = macListBuilder.ToString().TrimEnd();

            // Check: Did we find any valid MAC addresses?
            if (string.IsNullOrEmpty(macListString))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("No valid MAC addresses found.");
                return errorResponse;
            }

            // STEP 5: LOGIC: SWITCH
            string groupChoice = "";
            switch (data.GroupChoice)
            {
                case 1: groupChoice = "Staff - Device - MAC Addresses"; break;
                case 2: groupChoice = "Student - Device - MAC Addresses"; break;
                case 3: groupChoice = "Microsft Intune Devices"; break;
                case 4: groupChoice = "Securly Filtered - Device MAC Addresses"; break;
                default: groupChoice = "Staff - Device - MAC Addresses"; break;
            }

            // STEP 6: BUILD FINAL SCRIPT
            // Using string interpolation
            string finalScript = $@"config firewall address
edit ""{groupChoice}""
    config dynamic_mapping
        edit ""{data.FortigateName}""-""root""
        set associated-interface ""any""
        unset color
    set macaddr {macListString}
next
end";

            // STEP 7: RETURN RESPONSE
            // Build response object
            var responseObj = new MacResponse
            {
                Script = finalScript,
                Count = cleanMacList.Count,
                ExtractedMacs = cleanMacList
            };

            // In Isolated mode, we build the response manually like this:
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseObj);
            //await response.WriteStringAsync("Placeholder: It works!");

            return response;
        }
    }

    public class MacResponse
    {
        public string Script { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> ExtractedMacs { get; set; } = new List<string>();
    }
}

