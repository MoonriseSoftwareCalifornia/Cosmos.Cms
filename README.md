# Cosmos

##IMPORTANT: This repository is being archived as a major new release of Cosmos is now available.

Please visit the [new repository](https://github.com/CosmosSoftware/CosmosCms).

[![ubunto build](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/codeql-analysis.yml) 
[![NuGet Badge](https://buildstats.info/nuget/CDT.Cosmos.Cms.Common)](https://www.nuget.org/packages/CDT.Cosmos.Cms.Common/)
[![Unit Tests (67)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/unittests.yml/badge.svg)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/unittests.yml)

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FCosmosSoftware%2FCosmos.Cms%2Fmain%2FAutomation%2FAzure%2Fazuredeploy.json)

Quick links: [Getting Started](#getting-started-with-cosmos) | [Create Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) | [Create with VS Code](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md) | [Technical Documentation](https://cosmos.moonrise.net/documentation)

## Flexible Design

Cosmos is a lightweight CMS that has a built-in WYSIWYG and Source Code editor, and it also allows you to create single page applications externally with your favorite IDE, and when ready, import them. Cosmos supports your favorite framework such as Angular, jQuery, React, Vue, Google Maps, Map Box, ESRI, Bing Maps ...and others. 

See our [live framework demos](https://cosmos.moonrise.net/Demos).

Example Use Cases:

* High performance, light-weight CMS able to sustain large number of users.
* Host an Angular, jQuery, React or Vue web-app as a "home page" along with other pages with documentation.
* A dashboard website with graphs, charts, maps along with report pages and documentation.
* A CMS with an extremely easy HTML editing interface for non-technical users needing to make quick edits.

Cosmos was built by the staff of the California Department of Technology as an open, cloud-first high performance platform built to host websites during times of natural disasters and other emergencies. It was used from 2019 - 2021 for fire response, and in 2020 for COVID response.

The California Department of Technology decided to open source the project at the end of 2021.

## Getting Started with Cosmos

In under 15 minutes you can create a Cosmos website in Azure and get started exploring its capabilities.

Here is how:

* [Install Cosmos](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md)
* [Choose a Layout](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Import.md)
* [Create a home page](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/CreateHomePage.md)

## Create Content

There are three ways to create content that can be hosted on Cosmos:

* Use the built-in [HTML or WYSIWYG Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md) (best for non-technical folks)
* Use the built-in [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) (best for developers)
* Use [Visual Studio Code on your Desktop](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/Creating-with-VS-Code.md) with `npm build` and import directly into Cosmos. (best for building web-apps)

## Upload Assets

Use the file manager to upload one or more files at a time. You can also upload whole folders at once.

## Project Status

* Cosmos CMS began as a project within the [State of California's](https://www.ca.gov/) [California Department of Technology](https://cdt.ca.gov/), Office of Enterprise Technology from 2019 through 2021 and was built as a collaboration between it's DevOps and software engineering teams.
* Starting in December of 2021 management at the Department of Technology decided to make this project open-source.
* January 2022 this project officially "opened its doors" for business as an open-source project.
* In January of 2022 [Tek Yantra](https://tekyantra.com/) offered to sponsor this project and become a contributor.  Tek Yantra staff have been involved with this project from the begining.
* Collaboration between this project and staff from the California Department of Technology continues.
* [Product website](https://cosmos.moonrise.net/) launched.
* Now the project is in a phase where we are trying to grow our developer and user community.

### Why Cosmos CMS was created

This project began out of a need for a lean, easy to use, high performance Content Management System that has the capacity to handle extremely high number of users and be highly available--capable of hosting a single website from both Azure and AWS with real-time content synchronization between the two.

Cosmos CMS hosted websites built for the State of California's fire response of 2019 through 2021 and COVID 19 response of 2020.

[Please see our blog article](https://cosmos.moonrise.net/blog) that describes the unique architecture of this system.

## Help contribute

There are a number of ways you can help:

* Give Cosmos a try with our 10 minute [Azure Install](https://cosmos.moonrise.net/get_started/install).
* Provide us feedback through our online discussions:
  * [General discussion](https://github.com/CosmosSoftware/Cosmos.Cms/discussions/categories/general).
  * [Q & A](https://github.com/CosmosSoftware/Cosmos.Cms/discussions/categories/q-a).
  * [Ideas](https://github.com/CosmosSoftware/Cosmos.Cms/discussions/categories/ideas).
  * [Show and tell](https://github.com/CosmosSoftware/Cosmos.Cms/discussions/categories/show-and-tell) what you have done with Cosmos.
* Give this project a "Star" or click on "Watch"
* "Fork" this repository, and help us with code development and/or documentation.

# Developing with Cosmos

Would you like to fork one of our repos and contribute? See our [contributing guidelines](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/CONTRIBUTING.md) and [please see our documentation on how to get started](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/DevelopingWithCosmos.md) developing with Cosmos.

## Cosmos Repos

Three repositories are associated with this project:

* The [Cosmos](https://github.com/CosmosSoftware/Cosmos.Cms) repository contains the "Editor" and the "Publisher" and all the frameworks common to each.
* The "[Cosmos Publisher](https://github.com/CosmosSoftware/Cosmos.Cms.Publisher)" repository contains a stand alone "Publisher" website.
* [Open-source layouts](https://github.com/CosmosSoftware/Cosmos.Starter.Layouts) made ready for use with Cosmos CMS.

The publisher repository is a "stock" out of the box Visual Studio application. It is turned into a "Publisher" by adding and configuring the [Cosmos Common](https://www.nuget.org/packages/CDT.Cosmos.Cms.Common/) NuGet package.

# Installation

You can deploy Cosmos to Azure by clicking the following button (recommended) or manually install by following the directions below.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md)

This documentation is still under development, so check back for more topics as they become available.

## Docker Container Manual Install

* Setup Help
  * [Manual installation instructions](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/Index.md)
  * Links to Docker containers: [Editor](https://hub.docker.com/repository/docker/toiyabe/cosmoseditor) | [Publisher](https://hub.docker.com/repository/docker/toiyabe/cosmospublisher)
* [Developing with Cosmos](/Documentation/DevelopingWithCosmos.md)

