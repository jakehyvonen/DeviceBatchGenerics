# DeviceBatchGenerics

DeviceBatchGenerics is a C#/.NET library that enables automated report generation from LED voltage sweep test data.

Notable features include:
<ul>
  <li>.png plotting using OxyPlot</li>
  <li>spreadsheet reports using OfficeOpenXML</li>
  <li>OriginLab plotting/reporting using COM automation</li>
  <li>LED efficiency calculations and .csv reporting</li>
</ul>

The motivation for this project was to save time for LED researchers. Plotting data can be quite time consuming, especially when comparing data from different devices and tests. A lot of the plots that researchers desired can now be created with little effort.

This was all built on top of an Entity Framework library:
<a href="https://github.com/jakehyvonen/EFDeviceBatchCodeFirst">EFDeviceBatchCodeFirst</a>
