# Annotations for external libraries

This folder contains JetBrains code annotations in XML format, that apply to external libraries that we use.

An example for where this is very useful is to guide code analysis to not consider implicitly used types as unused,
such as for validators built using FluentValidation.

For info on how to write annotations,
see [the Rider documentation](https://www.jetbrains.com/help/rider/Code_Analysis__External_Annotations.html).

For a lot of examples of annotations
see [JetBrains standard collection of annotations](https://github.com/JetBrains/ExternalAnnotations)
