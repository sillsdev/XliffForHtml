# XliffForHtml

program for extracting xliff 1.2 from tagged html or injecting translations from xliff 1.2 into tagged html

## Tagging

Elements in the HTML with text that needs to be translated must be tagged with either a
data-i18n attribute or an i18n attribute.  The attribute value is used as an id in a trans-unit
element of the Xliff file.  (If both attributes exist, the data-i18n value is used.)  This
approach to localizing an HTML file differs greatly from that suggested by the
[OASIS XLIFF 1.2 Representation Guide for HTML](http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html),
but makes it easier to maintain the localization as the HTML file changes.

Note that attributes with text that needs to be translated cannot be handled by this program.
Only text nodes can be translated.  An effort is made to maintain inline markup, but that
requires the translator to know how to handle the strange looking XML in the middle of the text
data.  How this is displayed to the user depends totally on the editing environment.  The
"maximalist" syntax suggested by
[OASIS](http://docs.oasis-open.org/xliff/v1.2/xliff-profile-html/xliff-profile-html-1.2-cd02.html)
is followed for these inline elements.
