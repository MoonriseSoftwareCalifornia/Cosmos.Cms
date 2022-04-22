# Tutorial: Creating with VS Code ![VS Code Icon](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/vs-code-icon.png)

With Cosmos, you can develop rich web applications and content on your desktop and import them directly into Cosmos.

In this tutorial I will show how to...

1. [Create a web page](#step-1-create-a-web-page-using-cosmos) in Cosmos then export it to your desktop.
2. [Build an example web app](#step-2-create-an-angular-web-app-and-build-it) using Visual Studio Code, Angular, and npm.
3. [Then import](#step-3-import-the-web-page) the build into Cosmos.

## Prerequisites

A few things need to be installed on your computer before starting this tutorial.

1. You've [installed Cosmos](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md) and have  "[layout](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md)." 
2. [Visual Studio Code](https://code.visualstudio.com)
3. [Node.js](https://nodejs.org)

Let's begin...

## Step 1: Export your layout

The web app we are building will be hosted by Cosmos and the layout contains all the common elements for each page of your website.

Start by exporting your layout so you can encorporate it with your web app. 

Here is how:
 
 * Click the "Menu" button and select "Layouts."
 * Download the "default" layout by clicking the "export" button
 * Alternatively, click the download button for one of the other layouts you have.

Once the page downloads open it with your code editor (in our case Visual Studio Code).

Look at the HTML comments, and you will find what parts of the page are "uneditable" because they are part of the layout, and what parts are editable--this is where you put your web app.

## Step 2: Create an React web app

This example a React project called [Snap Shot](https://github.com/Yog9/SnapShot) found on [GitHub](https://github.com/Yog9/SnapShot).

It uses the following:

* React js
* React Router
* React Hooks
* Context API
* Flickr API

Follow the [getting started](https://github.com/Yog9/SnapShot#getting-started) instructions on the [Readme document](https://github.com/Yog9/SnapShot#snap-shot-) to install dependencies and build your application.

Run the app and you should see something like below.

![Example Web App Running](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SnapShotDemo.png)

A keen eye will notice that the application above does not include the "layout" elements of my [Cosmos website](https://cosmos.moonrise.net).

Specifically, the application is missing the header and footer elements.

Now we are going to get this application ready to import into Cosmos.

## Step 3: Merge the app with the exported layout

Now we are going to merge the app we just create with the layout.

Let's start with this:

* Locate the layout you exported from Cosmos and add it to your project.
* You should now see the file added to your `src` folder like below (your file name might be different).

![Exported file now in src folder](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-file-added.png)

Open the exported file in VS Code notice HTML comments within the `<head></head>` and the `<body></body>` of the page that mark areas as `(not editable)`.

Below is a screenshot of an example where you can see what the comments look like.

![HEAD screen shot](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-head-layout.png)

The content within these comments are part of the layout.  It is important to not edit anything within these blocks.

*IMPORTANT!: Do not remove the HTML comments as these are needed during the import process.*

Now complete the following steps:

* Rename your `index.html` page to something like `original-index.html` and rename the import page to `index.html'.
* Copy the code from  `original-index.html` and put it in your new `index.html` making sure the uneditable areas are not changed.
* Build and start your app to see the results.

## Step 4: Test and Debug your app

It is not unusual to have conflicts between the CSS and JavaScript functions of your layout and your application.  If you choose a "lean" layout you will minimize any problems.

Below is a screenshot of the finished app that now has the layout elements implemented, and I have resolved any CSS conflicts.

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





Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)
