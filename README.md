# Cosmos HL/CMS

[![ubunto build](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CosmosSoftware/Cosmos.Cms/actions/workflows/codeql-analysis.yml)

Cosmos is a high performance [web content management system](https://en.wikipedia.org/wiki/Web_content_management_system) with a hybrid design that combines a [static](https://en.wikipedia.org/wiki/Static_web_page) with a [dynamic](https://en.wikipedia.org/wiki/Dynamic_web_page) website.  The result is a _dynamic_ content management system that performes as well as a static site.

Content is managed by a third website, called the "Editor."  It maintains the files on the static website and makes edits to web pages on the dynamic website.  It also schedules when new content can appear, and optionally manages integration with CDNs.  Moreover it can synchronize content between Cosmos instances in real time.

Performance-wise Cosmos HL can match or slightly exceed that of static websites backed with either Amazon S3 or Azure Storage accounts.
 
Key features:

* A dynamic website that performs like a static one.
* Can host a single website simultaneously in AWS and Azure clouds with realtime-synchronization.
* Code editor for web developers and a WSYWIG editor for the non-technical.
* Cost effective to run.
* Optionally integrates with Akamai, Microsoft, and Verizon CDNs.

Two repositories are associated with this procject:

* The [Cosmos](https://github.com/CosmosSoftware/Cosmos.Cms) repository contains the "Editor" and the "Publisher" and all the frameworks common to each.
* The "[Cosmos Publisher](https://github.com/CosmosSoftware/Cosmos.Cms.Publisher)" repository contains a stand alone "Publisher" website.

The publisher repository is a "stock" out of the box Visual Studio application. It is turned into a "Publisher" by adding and configuring the [Cosmos Common](https://www.nuget.org/packages/CDT.Cosmos.Cms.Common/) NuGet package.

## Origin

If you would like to know why Cosmos HL was built the way it is now, please read [the origin story](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Origin.md).

## Getting Started

This documentation is still under development, so check back for more topics as they become available.

* Setup Help
  * [Installation instructions](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/Index.md)
  * Links to Docker containers containers: [Editor](https://hub.docker.com/repository/docker/toiyabe/cosmoseditor) | [Publisher](https://hub.docker.com/repository/docker/toiyabe/cosmospublisher)
* [Developing with Cosmos](/Documentation/DevelopingWithCosmos.md)

