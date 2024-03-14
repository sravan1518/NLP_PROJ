using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Storage.Blobs;
using ESGAutomation;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace EsgSurveyAutomation
{
    //program
    class Program
    {
        private const string TextAnalyticsEndpoint = "";
        private const string TextAnalyticsApiKey = "";
        private const string StorageConnectionString = "<YOUR_STORAGE_CONNECTION_STRING>";
        private const string ContainerName = "<YOUR_CONTAINER_NAME>";

        // Define survey questions
        private static readonly Dictionary<string, string> SurveyQuestions = new Dictionary<string, string>
        {
            { "Environmental Initiatives", "What environmental initiatives were mentioned?" },
            { "Social Initiatives", "What social initiatives were mentioned?" },
            { "Governance Practices", "What governance practices were mentioned?" }
        };

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: EsgSurveyAutomation <PDF_FILE_PATH>");
                return;
            }

            string pdfFilePath = args[0];

            // Initialize Text Analytics client
            var textAnalyticsClient = new TextAnalyticsClient(new Uri(TextAnalyticsEndpoint), new AzureKeyCredential(TextAnalyticsApiKey));

            // Initialize Blob Service Client
            var blobServiceClient = new BlobServiceClient(StorageConnectionString);

            // Process survey response
            await ProcessSurveyResponse(pdfFilePath, textAnalyticsClient, blobServiceClient);
        }

        static async Task ProcessSurveyResponse(string pdfFilePath, TextAnalyticsClient textAnalyticsClient, BlobServiceClient blobServiceClient)
        {
            // Read text from PDF file
            string pdfText = ReadPdfText(pdfFilePath);

            // Analyze sentiment and extract key phrases
            var analysisResult = await textAnalyticsClient.AnalyzeSentimentAsync(pdfText);
            var keyPhraseResult = await textAnalyticsClient.ExtractKeyPhrasesAsync(pdfText);

            // Answer survey questions
            var answers = AnswerSurveyQuestions(pdfText);

            // Store processed data in Azure Storage
            StoreProcessedData(analysisResult, keyPhraseResult, answers, pdfText, blobServiceClient);
        }

         static void StoreProcessedData(Response<DocumentSentiment> analysisResult, Response<KeyPhraseCollection> keyPhraseResult, Dictionary<string, string> answers, string pdfText, BlobServiceClient blobServiceClient)
        {
            // Store sentiment analysis result, key phrases, and survey answers along with the PDF text
            Console.WriteLine("Sentiment: {0}", analysisResult.Value.Sentiment);
            Console.WriteLine("Key Phrases: {0}", string.Join(", ", keyPhraseResult.Value));
            foreach (var kvp in answers)
            {
                Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
            }

            // Store the PDF text and processed data in Azure Blob Storage
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = blobContainerClient.GetBlobClient("processed_survey_response.txt");

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    // Write processed data to a MemoryStream
                    writer.WriteLine("PDF Text:");
                    writer.WriteLine(pdfText);
                    writer.WriteLine();
                    writer.WriteLine("Sentiment: {0}", analysisResult.Value.Sentiment);
                    writer.WriteLine("Key Phrases: {0}", string.Join(", ", keyPhraseResult.Value));
                    foreach (var kvp in answers)
                    {
                        writer.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
                    }

                    writer.Flush();
                    stream.Position = 0;

                    // Upload processed data to Azure Blob Storage
                    blobClient.Upload(stream);
                }
            }
        }
       

        static string ReadPdfText(string pdfFilePath)
        {
            // Extract text from PDF file
            using (var document = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.ReadOnly))
            {
                string text = "";
                foreach (PdfPage page in document.Pages)
                {
                    text += page.ExtractText();
                }
                return text;
            }
        }

        static Dictionary<string, string> AnswerSurveyQuestions(string pdfText)
        {
            var answers = new Dictionary<string, string>();

            // Find answers to each survey question in the PDF text
            foreach (var kvp in SurveyQuestions)
            {
                string answer = FindAnswer(pdfText, kvp.Value);
                answers.Add(kvp.Key, answer);
            }

            return answers;
        }

        static string FindAnswer(string pdfText, string question)
        {
            // Implement logic to find answer to each survey question in the PDF text
            // This can be done using string manipulation, regular expressions, or natural language processing techniques
            // For simplicity, we'll just search for the question text and return the following sentence

            int index = pdfText.IndexOf(question, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                int startIndex = index + question.Length;
                int endIndex = pdfText.IndexOf('.', startIndex);
                if (endIndex != -1)
                {
                    return pdfText.Substring(startIndex, endIndex - startIndex + 1);
                }
            }

            return "Answer not found";
        }

      
            }
        }


// PROCESS TO EXECUTE
//Run the compiled executable file (EsgSurveyAutomation.exe) from the command line.
//Pass the path to the PDF file containing the survey responses as a command-line argument. For example:

//EsgSurveyAutomation.exe C:\path\to\survey_responses.pdf
//Output:

//The application will process the survey responses from the provided PDF file.
//It will output the sentiment analysis result, key phrases extracted from the survey responses, and answers to the survey questions to the console.
//Additionally, the processed data will be stored in Azure Blob Storage with the filename processed_survey_response.txt


//Replace the placeholder values (<YOUR_TEXT_ANALYTICS_ENDPOINT>, <YOUR_TEXT_ANALYTICS_API_KEY>, <YOUR_STORAGE_CONNECTION_STRING>, <YOUR_CONTAINER_NAME>) with your actual Azure service credentials and adjust the code as needed based on your specific requirements. Then, follow the steps mentioned earlier to run the code and obtain the output, which now includes answers to predefined survey questions based on the provided PDF file.


//EsgSurveyAutomation.exe C:\path\to\survey_responses.pdf