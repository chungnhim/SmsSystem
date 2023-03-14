using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IFileService
    {
        Task<string> SendMyFileToS3Async(Stream data, string subDirectoryInBucket, string fileNameInS3);
    }
    public class FileService : IFileService
    {
        private readonly AppSettings _appSettings;
        private readonly IDateTimeService _dateTimeService;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger _logger;

        public FileService(IOptions<AppSettings> appSettings, SmsDataContext smsDataContext, IDateTimeService dateTimeService, IAuthService authService, IUserService userService, ILogger<FileService> logger)
        {
            _dateTimeService = dateTimeService;
            _authService = authService;
            _userService = userService;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task<string> SendMyFileToS3Async(Stream data, string subDirectoryInBucket, string fileNameInS3)
        {
            var accessKey = _appSettings.S3Config.AccessKey;
            var secretKey = _appSettings.S3Config.SecretKey;
            var bucketName = _appSettings.S3Config.BucketName;
            var subFolder = _appSettings.S3Config.SubFolder;
            var regionEndpoint = Amazon.RegionEndpoint.APSoutheast1;

            using (IAmazonS3 client = new AmazonS3Client(accessKey, secretKey, regionEndpoint))
            {
                using (TransferUtility utility = new TransferUtility(client))
                {
                    TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
                    if (subDirectoryInBucket == "" || subDirectoryInBucket == null)
                    {
                        request.BucketName = bucketName;
                    }
                    else
                    {
                        request.BucketName = bucketName + @"/" + subFolder + @"/" + subDirectoryInBucket;
                    }
                    request.Key = fileNameInS3;

                    request.InputStream = data;
                    request.CannedACL = S3CannedACL.PublicRead;
                    await utility.UploadAsync(request);
                }
            }
            _logger.LogInformation($"File {fileNameInS3}  was upload to S3");

            return $"https://{bucketName}.s3.amazonaws.com/{subFolder}/{subDirectoryInBucket}/{fileNameInS3}";
        }
    }
}
