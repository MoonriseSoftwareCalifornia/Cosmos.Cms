# Installation

The type of Cosmos installation depends on what your needs my be. For many situations a basic installation may suffice,
but if higher performance or higher availability is required then CDN and other options might be considered.

Note: For installations on Microsoft Azure, please consider using our [automated quick install](https://cosmos.moonrise.net/get_started/install).  It takes about 10 minutes to run.

Begin your installation by following our basic installation instructions. This will install a database, file storage and
two web applications--one for the "Publisher" and another for the "Editor."

## Manual Installation

The basic install requires five resources to be deployed for Cosmos include the following: 

* SendGrid Account
* MS SQL Database
* Blob storage account
* Two "docker containerized" websites:
  * Publisher Website
  * Editor Website

An explanation of the architecture behind Cosmos can be found [on the product website](https://cosmos.moonrise.net/blog).

## Installation Order and Requirements.

### SendGrid Account

A [SendGrid account](https://docs.sendgrid.com/for-developers/partners) is needed by the Cosmos Editor for sending Emails related to user account management services. For most first-time installations the "free" SendGrid account will do.  You can obtain a free account through [Amazon Market Place, Google, Jetlastic, and Microsoft Azure](https://docs.sendgrid.com/for-developers/partners).

Tip:
* By default, SendGrid restricts address to an account [through IP Address "Allow Lists."](https://docs.sendgrid.com/ui/account-and-settings/ip-access-management). To simplify installation, this security feature can be disabled by clicking the "Disable Allow List" button on the IP Address Management console screen.  For "high-value" or "high-risk" scenarios, it is highly recommended to leave the IP Address Allow List enabled.
* As of this writing, it may be necessary to deploy the SendGrid account manually--not through using Terraform or other automation technology.

### File Storage

Files are stored in either AWS S3 or Azure Storage accounts with the public website enabled.

* [AWS S3 with public website](https://docs.aws.amazon.com/AmazonS3/latest/userguide/HostingWebsiteOnS3Setup.html). 
* [Azure Storage with public website](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website).

IMPORTANT:
* The storage account API needs to be accessible to the Editor Website.
* IMPORTANT! For Amazon S3 you will need to [enable SSL for the S3 website](https://aws.amazon.com/premiumsupport/knowledge-center/cloudfront-serve-static-website/). This can be done through CloudFront or a load balancer.

### MS SQL Database

Shown below are links that show how to deploy a database to AWS and Azure.

IMPORTANT! The database has to be accessible to both the Publisher and Editor websites.

* [Amazon RDS For SQL Server](https://aws.amazon.com/rds/sqlserver/)
* [Azure SQL Server](https://azure.microsoft.com/en-us/products/azure-sql/database/)

### Editor and Publisher Website Docker Container Installation

Cosmos requires two websites to be deployed as Docker Containers. It is recommended to install these first as the docker containers take a while to download and spin up.  By the time the storage and database resources are deployed, these websites will be ready to use.

#### Editor Website

The purpose of the "Editor" is to manage content.  Here are the particulars about the installation:

* The Docker Container can be found on [Docker Hub](https://hub.docker.com/repository/docker/toiyabe/cosmoseditor).
* The Editor Website needs the following settings enabled:
  * Affinity Bit Enabled
  * Web Sockets Allowed

The editor website needs the following "Secrets" to be set:

* CosmosAdminEmail [Email address of the person who will be the administrator of this website]
* CosmosBlobContainer [Can be either '$web' for Azure, or the name of the S3 container for Amazon]
* CosmosEditorUrl [The publick URL of the editor website]
* CosmosPrimaryCloud [This is the cloud where this installation is located. It can be amazon, azure, or google]
* CosmosPublisherUrl [The public URL to the Publisher website]
* CosmosSendGridApiKey [SendGrid API Key that is enabled to send email]
* CosmosStorageUrl [The URL to the public website for AWS S3 or Azure Storage Account]

For Amazon Web Services the following environment variables will be needed:

* AmazonAwsAccessKeyId [API Access key ID]
* AmazonAwsSecretAccessKey [The actual access key]
* AmazonRegion [Amazon region where deployed]
* ConnectionStrings_DefaultConnection [This is the Microsoft SQL Connection String to the database and database server]

For Microsoft Azure you will need the following configuration variables:

* storageAccountName [Azure Storage Account or S3 Bucket Name]
* ConnectionStrings:
  * DefaultConnection [This is the Microsoft SQL Connection String to the database and database server]
  * BlobConnection [Connection string to the Azure Storage Account]

#### Publisher Website

The publisher installation is relatively simple as it's sole purpose is the "publish" content on the web. By design it is light-weight and "lean."

* The Docker Container for the publisher can be found on [Docker Hub](https://hub.docker.com/repository/docker/toiyabe/cosmospublisher).
* The Editor Website needs the following settings enabled:
  * Affinity Bit DISABLED (IMPORTANT! For performance reasons DISABLE the affinity bit for the Publisher)

The publisher needs the following "Secrets" set:

For Amazon Web Services the following environment variables will be needed:
* CosmosPrimaryCloud [Set to: amazon]
* ConnectionStrings_DefaultConnection [This is the Microsoft SQL Connection String to the database and database server]

For Microsoft Azure you will need the following configuration variables:

* CosmosPrimaryCloud [Set to: azure]
* ConnectionStrings:
  * DefaultConnection [This is the Microsoft SQL Connection String to the database and database server]

## Performance Tips

For optimal performance, do not install the "Publisher" and "Editor" websites on the same resource.  Keep them separate.  For performance let the "Publisher" resource scale out rapidly and draw back slowly.  For example, scale out 2 instances at a time when a certain CPU threashold is hit, and then, reduce by 1 instance at a time as CPU usage decreases.

The publisher and editor has been tested with Azure Front Doort Firewall. This firewall uses OWASP rules.  Both the publisher and editor function well with all OWASP rules turned on.  This means other firewalls that are also using OWASP rules should work fine in full "prevention mode" with Cosmos.

## CDN

Cosmos is capabilbe of integrating with Akamai Fast Purge, or through Microsoft Azure: Akamai, Microsoft and Verizon CDN's.
