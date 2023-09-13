using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace CodeSigningHelper
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static IAmazonS3 _s3Client;
        private static string _signedBucketName;
        private static string _unsignedBucketName;

        private static readonly TimeSpan DefaultTimeOut = TimeSpan.FromMinutes(5);

        private const string Prefix = "CoreWCFExtensionsAuthenticodeSigner/AuthenticodeSigner-SHA256-RSA";
        private const string SignerJobIdTag = "signer-job-id";

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Code Signing Helper");

                _s3Client = new AmazonS3Client(new EnvironmentVariablesAWSCredentials());

                Console.WriteLine("----------------");

                _unsignedBucketName = args[0];
                _signedBucketName = args[1];

                var token = new CancellationTokenSource(DefaultTimeOut).Token;

                await Sign(args.Skip(2), token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                Console.WriteLine(Environment.NewLine);

                Console.WriteLine(
                    $"Usage: {Assembly.GetExecutingAssembly().GetName().Name} "
                        + $"<unsignedBucketName> <signedBucketName> <workingDirectory1> ... <workingDirectoryN>"
                );

                // signal failure to signing pipeline
                Environment.Exit(-25);
            }
        }

        static async Task Sign(IEnumerable<string> workingDirectories, CancellationToken token)
        {
            workingDirectories ??= new List<string>();

            var tasks = workingDirectories.Select(dir => Sign(dir, token)).ToArray();

            await Task.WhenAll(tasks);
        }

        static async Task Sign(string workingDirectory, CancellationToken token)
        {
            var files = Directory.GetFiles(workingDirectory, "*.dll", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                Log($"Begin {fileName}");

                var unsignedKey = Path.Join(Prefix, Path.GetFileName(file));

                Log($"Putting Object [{unsignedKey}] to [{_unsignedBucketName}]");

                await _s3Client.PutObjectAsync(
                    new PutObjectRequest
                    {
                        FilePath = file,
                        BucketName = _unsignedBucketName,
                        Key = unsignedKey
                    },
                    token
                );

                Log($"Uploaded {fileName}.  Waiting for SignerJobId Tag");

                string? signerJob = null;
                do
                {
                    var tags = await _s3Client.GetObjectTaggingAsync(
                        new GetObjectTaggingRequest { BucketName = _unsignedBucketName, Key = unsignedKey },
                        token
                    );

                    signerJob = tags.Tagging.FirstOrDefault(t => t.Key == SignerJobIdTag)?.Value;
                } while (string.IsNullOrEmpty(signerJob));

                Log($"Found Signer Job Id for {fileName}: [{signerJob}].  Monitoring Signed Bucket");

                var signedKey = Path.Join(Prefix, $"{fileName}-{signerJob}");

                GetObjectResponse signedResult;
                while (true)
                {
                    try
                    {
                        signedResult = await _s3Client.GetObjectAsync(
                            new GetObjectRequest { BucketName = _signedBucketName, Key = signedKey },
                            token
                        );

                        break;
                    }
                    catch (AmazonS3Exception e)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1500), token);
                    }
                }

                Log($"Found signed file for {fileName}.  Downloading");

                await signedResult.WriteResponseStreamToFileAsync(file, append: false, cancellationToken: token);

                Log($"Wrote signed file to {file}");

                // Don't have permissions to cleanup _unsignedBucketName
                // Log($"Deleting {file} from [{_unsignedBucketName}]");
                //
                // await _s3Client.DeleteObjectAsync(
                //     new DeleteObjectRequest { BucketName = _unsignedBucketName, Key = unsignedKey },
                //     token
                // );
            }
        }

        static void Log(string message)
        {
            Console.WriteLine(
                $"[{DateTime.Now:T} T-{Thread.CurrentThread.ManagedThreadId.ToString(format: "D2")}]: {message}"
            );
        }
    }
}
