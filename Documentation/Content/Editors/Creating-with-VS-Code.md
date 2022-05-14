# Tutorial: Build a [React web app](https://github.com/Yog9/SnapShot) then import it into Cosmos

Cosmos enables web developers to create applications outside of Cosmos, then import them to host and manage. This allows web applications built with Angular, jQuery, React, Vue or other framework to be hosted on the same website, along with traditional HTML content.

This tutorial shows how to build the sample React web application called "[SnapShot](https://github.com/Yog9/SnapShot)" outside of Cosmos, then how to import it.

More specifically, this tutorial will take you through the following:

1. [Export a layout](#step-1-export-your-layout)
2. [Build an example web app](#step-2-create-an-angular-web-app-and-build-it) using Visual Studio Code, React, and npm.
3. [Then import](#step-3-import-the-web-page) the application into Cosmos to host.

Here is a [link to a live result on our website](https://cosmos.moonrise.net/demos/snapshot).

## Prerequisites

To follow along you will need:

1. [Cosmos installed](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Installation/AzureClickInstall.md)
2. [Visual Studio Code](https://code.visualstudio.com) installed on your computer.
3. [Node.js](https://nodejs.org) is installed on your computer.
4. [Yarn](https://yarnpkg.com/getting-started/install) package manager if not already installed with npm.
5. Know how to edit HTML, JavaScript and CSS.

## Step 1: Export your layout

[Layouts](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md) contain all the common elements that appear on each page of your website. In this tutorial our example application will be embedded in our layout. This means we will need to export the layout from Cosmos so we can encorporate it with our application.

To export the default layout of your website follow these steps:
 
 * Log into the "Editor" for your website and click the "Menu" button, then select "Layouts."
 * Download the "default" layout by clicking the "export default" button.

![Export default layout button](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-layout-button.png)

Remember the download location as you will need to copy the layout into your project later.

## Step 2: Create the React web application

The current example application is a React project called [Snap Shot](https://github.com/Yog9/SnapShot) found on [GitHub](https://github.com/Yog9/SnapShot) that makes use of the following:

* React js
* React Router
* React Hooks
* Context API
* Flickr API

Clone the application on your computer and take a moment to read the [getting started](https://github.com/Yog9/SnapShot#getting-started) instructions on the [Readme document](https://github.com/Yog9/SnapShot#snap-shot-) to install dependencies and build your application.

After cloning the application you may want to do some house keeping such as running `npm audit` as shown below to get things updated.

```shell
yarn install
npm audit -fix
```

Try running the application using the command below to view the application. A screen shot of what you should see is given below.

```shell
yarn start
```

![Example Web App Running](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/SnapShotDemo.png)

## Step 3: Merge the app with the exported layout

We want this application to be hosting within the [Cosmos product website](https://cosmos.moonrise.net)--which has a standard *header* and *footer* on each page. Now it is time merge the SnapShot application with the layout so my web app looks like it belong on the Cosmos website.

To begin, locate the layout you exported from Cosmos and add it to your project. You should now see the file added to your `src` folder like below (your file name might be different).

![Exported file now in src folder](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-file-added.png)

Open the exported file in VS Code notice HTML comments within the `<head></head>` and the `<body></body>` of the page that mark areas as `(not editable)`.

Below is a screenshot of an example where you can see what the comments look like.

![HEAD screen shot](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-export-head-layout.png)

&nbsp;&nbsp;&nbsp;&nbsp; *IMPORTANT: Do not edit or alter anything contained within areas marked as not editable.*

Now complete the following steps:

* Rename your `index.html` page to something like `original-index.html`.
* Rename the layout file to `index.html'.
* Copy the React code from  `original-index.html` and put it in your new `index.html` making sure the uneditable areas are not changed.
* Build and start your app to see the results.

Things to keep in mind as you merge your application with your layout:

* Remember the "layout wins."  So remove tags from your application that already occur in the "layout" blocks.
* It is not uncommon that CSS conflicts may exist between your layout and application. This is OK as most times these things are easily fixed.

## Step 4: Test and Debug your app

Use the following command to start the application.

```shell
yarn start
```

At this point it is not uncommon to have CSS conflicts with your layout. Fix those now. (See our [tips](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/About.md#tips)] for avoiding layout conflicts.

Below is a screenshot of the finished app that now has the layout elements implemented.

![Finished App Screenshot](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-ready-to-import.png)

Now we are ready to import this into Cosmos.

## Step 5: Import the app

The import process happens in four steps:

1. Create the deployment package.
2. Upload assests to Cosmos.
3. Import the `index.html` page.
4. Correct JavaScript and CSS paths in web page.

### Create the deploy package

Run the `yarn build` command to "build" the application and create the package.

### Uploading assets using file manager

Login to the Cosmos Editor for your website, then select "Menu" then "Files."  This will open the *file manager*.

Create a folder to hold your application's assets.  I created the following folder `/pub/apps/snapshot` as show in the picture below.

![File Manager](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-filemanager.png)

Here are the steps to upload the files:

1. Using *file manager* click on the folder you just created and then select "upload files."
2. Open the folder on your computer where the deployment package exists. You should see something like below.

![Files on computer selected and ready for import](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/tutorial1-files-selected-for-import.png)

4. Select all these files *except index.html*, plus the `static` folder, then *drag and drop* them on the uploader.
5. After files have uploaded click the "close" button.

Using the file manager, check what was just uploaded.

### Import the web page

Next we need to import the `index.html` into Cosmos. Here are the steps:

1. Click the "Menu" button then select "Pages."
2. Either create a new page to hold the application or select an existing page and click "Code Editor."
3. Once the code editor opens, select "Menu" then "Import."
4. The import dialog open, click the "Select files" button, and select the `index.html` file in the "build" folder (../build/index.html).

Once the document loads, the import tools closes and now the new application code should be revealed in your online code editor.

One more step:

### Fix paths

When creating this application the index.html has links referencing JavaScript, CSS and other asset files to include. Note the paths. They may be relative to your development environment.

But now these assets have been uploaded to the Cosmos website and the URLs to these files are now likely to be difference. You will need to change them.

Here are some examples (Note: Your "New URL" may be different than below):

Old URL: /SnapShot/manifest.json
New URL: /pub/apps/snapshot/manifest.json

Old URL: /SnapShot/static/css/main.e41d18ea.chunk.css
New URL: /pub/apps/snapshot/static/css/main.e41d18ea.chunk.css

Now when you preview the application, it should look and work fine. You can see the finished example [above online](https://cosmos.moonrise.net/demos/snapshot).

This concludes this tutorial.  If you have any questions, please feel free to post in our [Q&A discussion section](https://github.com/CosmosSoftware/Cosmos.Cms/discussions/categories/q-a).

---

Previous: [Create and Edit Content](https://github.com/CosmosSoftware/Cosmos.Cms/tree/main/Documentation/Content) Next: [Code Editor](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Content/Editors/CodeEditor.md) Releated: [HTML (WYSIWYG) Editor](https://github.com/CosmosSoftware/Cosmos.Cms/edit/main/Documentation/Content/Editors/WYSIWYG(HTMLEditor).md)
