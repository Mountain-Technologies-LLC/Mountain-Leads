using System;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Constructs;

namespace Infrastructure.Constructs
{
    internal class BucketDeploymentConstructProps : IStackProps
    {
        public Bucket Bucket;
        public Distribution distribution;
    }

    public class BucketDeploymentConstruct : Construct
    {
        internal BucketDeploymentConstruct(Construct scope, string id, BucketDeploymentConstructProps props = null) : base(scope, id)
        {
            _ = new BucketDeployment(
                this, "s3BucketDeploy",
                new BucketDeploymentProps
                {
                    Sources = [Source.Asset("./website/bin/Release/net10.0/publish/wwwroot")],
                    DestinationBucket = props.Bucket,
                    Distribution = props.distribution,
                    DistributionPaths = ["/*"],
                    MemoryLimit = 1024,
                    EphemeralStorageSize = Size.Mebibytes(1024)
                });

            // Output the website URL
            var bucketUrl = props.Bucket.BucketWebsiteUrl;
            Console.WriteLine($"Bucket URL: {bucketUrl}");
        }
    }
}
