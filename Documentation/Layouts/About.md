# About Layouts

Layouts bring visual consistency across all pages of your website.  For example you can add a common navigation banner or footer to all your pages, set common CSS styles for things like headings, paragraphs, colors for page backgrounds, links, text, and things like Google Analytics settings.

You can define your own layouts and include as much or as little as you need. Or, choose not to use layouts at all if each page of your website is going to be distinct or unique.

There are a number of pre-built, open-source "[starter templates](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/StarterLayouts.md)" for you to use right away. Each can be [imported directly into Cosmos](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Import.md).

## Layout Structure

You can create your own layout if needed. Here is some information to get you started:

The structure of a layout is fairly open and it is highly customizable. There are no requirements to use one framework or another such as Bootstrap, jQuery, Zerb, Bulma, Pure or any of the others. Build your layout using the framework your most comfortable with.

A layout injects content into certain parts of each web page:

* At the top of the `<head></head>`. This is a good place to include CSS links, JavaScript libraries that will need to be on every page your your website.
* At the top of the `<body></body>`. This is best used if you are going to include a common banner and/or navigation bar on each page.
* At the bottom of the `<body></body>`. Here is a good place to inject a footer and/or links to JavaScript libraries at the bottom of each page.

Note: You can decide to use all these injects, or some, or none at all by leaving each inject empty when not in use.

## Tips

### Use generic layouts
Cosmos can host applications built with multiple frameworks such as Angular, jQuery, React and Vue all on the same website. If doing so use a layout that uses "plain JavaScript and CSS" so it reduces the chance for conflicts with your other frameworks in use.

### Changing Layouts
You can change your layout at any time. WARNING! This can have unforseen consequences on your website. Proceed with care!

Related: [Starter Layouts](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/StarterLayouts.md) | [Import a Layout](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Import.md) | [Create a Layout](https://github.com/CosmosSoftware/Cosmos.Cms/blob/main/Documentation/Layouts/Create.md)
