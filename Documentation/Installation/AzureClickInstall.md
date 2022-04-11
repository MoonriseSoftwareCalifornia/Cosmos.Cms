# Install Cosmos to Azure

Prerequisites:

* You already have an [Azure account](https://azure.microsoft.com/en-us/free/search/?OCID=AID2200277_SEM_0bab6efa957c19e686fb0f63b7658f30:G:s&ef_id=0bab6efa957c19e686fb0f63b7658f30:G:s&msclkid=0bab6efa957c19e686fb0f63b7658f30) and know how to navigate the Azure Portal website.
* If you do not already have one, please [create a SendGrid account](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/SendGrid.md) then [create and save an API key](https://docs.sendgrid.com/for-developers/partners/microsoft-azure-2021#api-keys). You will need the key during the install.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FCosmosSoftware%2FCosmos.Cms%2Fmain%2FAutomation%2FAzure%2Fazuredeploy.json)

This button installs the following in about 15 minutes:

* [Azure SQL Database](https://azure.microsoft.com/en-us/products/azure-sql/database/) (SKU Basic)
* Storage Account ([Standard RA-GRS](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-overview))
* Linux [App Service Plan](https://docs.microsoft.com/en-us/azure/app-service/overview-hosting-plans) (SKU B1)
  * Cosmos Editor Website ([container from Docker Hub](https://hub.docker.com/repository/docker/toiyabe/cosmoseditor))
  * Cosmos Publisher Website ([container from Docker Hub](https://hub.docker.com/repository/docker/toiyabe/cosmospublisher))

PLEASE NOTE: The Docker containers may take 5 minutes to install and run upon first use.  This is normal.

## Required Post Installation Steps

These steps take about 5 minutes to complete:

* Click the "Outputs" tab on left of the install page after the template finishes.
* Copy the list of IP addresses and [add them your SendGrid account](https://docs.sendgrid.com/ui/account-and-settings/ip-access-management).
* Open the Editor website and create your admin account.
  * Note: The Editor and Publisher websites may take a minute or two to spin up as the Docker containers are being installed upon first use.
* After creating the admin account, remove the configuration variable "CosmosAllowSetup" from the Editor website.
* [Enable the static website](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website-host#configure-static-website-hosting) on the storage account.
* Close all web browsers.
* Open a new web browser and log into the Editor to begin building your website.
 

## Optional (yet recommended) Post Installation Steps

* This install chooses DNS names automatically for the editor, publisher, and the storage websites. Now you may want your own custom DNS names for each. After adding your own DNS names, you will have to update the configuration of your Cosmos install.
  * [Setup custom domain for Editor and Publisher App Services](https://docs.microsoft.com/en-us/Azure/app-service/app-service-web-tutorial-custom-domain?tabs=cname)
  * [Setup custom domain for static website](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-custom-domain-name?tabs=azure-portal#map-a-custom-domain-with-https-enabled).
* The automated install deploys a low-cost implementation of Cosmos which is suitable for very light traffic.  It is recommended that you [scale up](https://docs.microsoft.com/en-us/azure/app-service/manage-scale-up) and [scale out](https://docs.microsoft.com/en-us/azure/azure-monitor/autoscale/autoscale-get-started?toc=/azure/app-service/toc.json) the App Service Plan and [Azure SQL instance](https://docs.microsoft.com/en-us/azure/azure-sql/database/scale-resources) to meet your needs.

___
Next Topic: [Choose a Layout](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Import.md) | Related: [Manual Install](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/Index.md)
