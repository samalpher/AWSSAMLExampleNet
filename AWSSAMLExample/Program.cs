using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;

namespace AWSSAMLExample
{
    class Program
    {
        public static void Main()
        {
            const string principleArn = "<identity provider ARN>";
            const string roleArn = "<role ARN>";

            var example = new Example(principleArn, roleArn);

            foreach (S3Bucket bucket in example.GetBuckets())
            {
                Console.WriteLine("BucketName: " + bucket.BucketName + " Created: " + bucket.CreationDate);
            }
        }
    }

    class Example
    {
        string principleArn;
        string roleArn;

        public Example(string principleArn, string roleArn)
        {
            this.principleArn = principleArn;
            this.roleArn = roleArn;
        }

        public List<S3Bucket> GetBuckets()
        {
            using (var s3Client = new AmazonS3Client(GetCredentials(), RegionEndpoint.USEast1))
            {
                return s3Client.ListBuckets().Buckets;
            }
        }

        private AWSCredentials GetCredentials()
        {
            const string profileName = "example_profile";
            const string endpointName = profileName + "_endpoint";
            const string samlEndpointUrl = "https://<adfs host>/adfs/ls/IdpInitiatedSignOn.aspx?loginToRp=urn:amazon:webservices";

            //Create and register our saml endpoint that will be used by our profile
            var endpoint = new SAMLEndpoint(
                endpointName, 
                new Uri(samlEndpointUrl), 
                SAMLAuthenticationType.Negotiate);

            var endpointManager = new SAMLEndpointManager();

            endpointManager.RegisterEndpoint(endpoint);

            //Use the default credential file.  This could be substituted for a targeted file.
            var netSdkFile = new NetSDKCredentialsFile();

            CredentialProfile profile;
            //See if we already have the profile and create it if not
            if (netSdkFile.TryGetProfile(profileName, out profile).Equals(false))
            {
                var profileOptions = new CredentialProfileOptions
                {
                    EndpointName = endpointName,

                    //This was kind of confusing as the AWS documentation did not say that this was 
                    //a comma separated string combining the principle ARN (the ARN of the identity provider) 
                    //and the ARN of the role.  The documentation only shows that it's the ARN of the role.
                    RoleArn = principleArn + "," + roleArn
                };

                profile = new CredentialProfile(profileName, profileOptions);
                profile.Region = RegionEndpoint.USEast1;

                //Store the profile
                netSdkFile.RegisterProfile(profile);
            }
            
            return AWSCredentialsFactory.GetAWSCredentials(profile, netSdkFile);
        }
    }
}
