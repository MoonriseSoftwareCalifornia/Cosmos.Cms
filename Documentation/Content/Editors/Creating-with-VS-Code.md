# Tutorial: Build a [React web app](https://github.com/Yog9/SnapShot) with [VS Code](https://code.visualstudio.com/) and [npm](https://www.npmjs.com/) then import and host it with Cosmos

Cosmos gives web developers maximum flexibility by enabling you to create and build applications outside of Cosmos, then import them into Cosmos to host and manage. In this tutorial I show how with the React web application "[SnapShot](https://github.com/Yog9/SnapShot)."  I use Visual Studio Code with npm on a local development computer to download and build the source code, then import the build into Cosmos. :Here is what we will cover

1. [Export a layout](#step-1-export-your-layout)
2. [Build an example web app](#step-2-create-an-angular-web-app-and-build-it) using Visual Studio Code, React, and npm.
3. [Then import](#step-3-import-the-web-page) the application into Cosmos to host.

Here is a [link to a live result on our website](https://cosmos.moonrise.net/Demos/SnapShot).

## Prerequisites

To follow along you will need:

1. [Cosmos is installed ](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md)
2. [Visual Studio Code](https://code.visualstudio.com)
3. [Node.js](https://nodejs.org) is installed on your development computer.
4. You know how to edit HTML, JavaScript and CSS
5. Know how to use [npm](https://docs.npmjs.com/cli/v6/commands/npm-build?msclkid=2ec764f0c49f11ec841c784956bfacb5) to build an application

## Step 1: Export your layout

[Layouts](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md) contains all the common elements that appear on each page of your website. Layouts enable you to be "[DRY](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself)" (don't repeat yourself). For this tutorial we want the web app to be placed within the website layout. Our first step then, is to export the layout so we can embed our application within it.

To export the layout do the following:
 
 * Log into the "Editor" for your website and click the "Menu" button, then select "Layouts."
 * Download the "default" layout by clicking the "export" button

![Layout file displayed in Windows Explorer](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/LayoutExported.png)

Open the *blank layout* your code editor (in our case Visual Studio Code) and notice the HTML comments noted as "uneditable.".

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; __IMPORTANT!__ _Do not edit or alter_ the uneditable sections as they are layout elements.

## Step 2: Create the React web application

Now close that file so we can start building the React application.This example a React project called [Snap Shot](https://github.com/Yog9/SnapShot) found on [GitHub](https://github.com/Yog9/SnapShot) that makes use of the following:

* React js
* React Router
* React Hooks
* Context API
* Flickr API

Read the [getting started](https://github.com/Yog9/SnapShot#getting-started) instructions on the [Readme document](https://github.com/Yog9/SnapShot#snap-shot-) to install dependencies and build your application.

Run the app and you should see something like below.

![Example Web App Running](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SnapShotDemo.png)

## Step 3: Merge the app with the exported layout

I want this application to be hosting within my website--which has a standard *header* and *footer* on each page. Notice that right now the application above does not include the standard header of my [website](https://cosmos.moonrise.net).  Now we are going to merge the app we just create with the layout so my web app looks like it belong in my website.

Let us start with this:

* Locate the layout you exported from Cosmos and add it to your project.
* You should now see the file added to your `src` folder like below (your file name might be different).

![Exported file now in src folder](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-file-added.png)

Open the exported file in VS Code notice HTML comments within the `<head></head>` and the `<body></body>` of the page that mark areas as `(not editable)`.

Below is a screenshot of an example where you can see what the comments look like.

![HEAD screen shot](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-head-layout.png)

The content within these comments are part of the layout.  It is important to not edit anything within these blocks.

Now complete the following steps:

* Rename your `index.html` page to something like `original-index.html`.
* Rename the layout file to `index.html'.
* Copy the React code from  `original-index.html` and put it in your new `index.html` making sure the uneditable areas are not changed.
* Build and start your app to see the results.

## Step 4: Test and Debug your app

At this point it is not uncommon to have CSS conflicts with your layout. Fix those now. (See our [tips](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md#tips)] for avoiding layout conflicts.

Below is a screenshot of the finished app that now has the layout elements implemented.

![Finished App Screenshot](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-ready-to-import.png)

Now we are ready to import this into Cosmos.

## Step 5: Import the app

The import process happens in four steps:

1. Run the command `npm run build` to create the run package.
2. Upload assests to Cosmos
3. Import the web page
4. Correct JavaScript and CSS paths in web page

### Uploading assets

Login to the Cosmos Editor for your website, then select "Menu" then "Files."  This will open the file manager.

Create a folder to hold your application's assets.  I created the following folder `/pub/apps/snapshot` as show in the picture below.

![File Manager](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-filemanager.png)

Here are the steps to upload the files:

1. With your folder selected, click the "upload files" button.
2. Open the folder on your computer where the files exist.
3. Select all these files *except index.html*, plus the `static` folder, and drop it on the uploader.
4. After files have uploaded click the "close" button.

Using the file manager, check what was just uploaded.

### Import the web page

Next we need to import the `index.html` into Cosmos. Here are the steps:

1. Click the "Menu" button then select "Pages."
2. Find the page you created above and click "Edit." On the next page select "Code View."
3. Once the code editor opens, select "Menu" then "Import."
4. The import dialog open, click the "Select files" button, and select the `index.html` file in the "build" folder.

### Fix paths

When developing this application

Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)
