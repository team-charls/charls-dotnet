# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

### Added

- New property JpegLSDecoder.CompressedDataFormat to retrieve the compressed data format of
the JPEG-LS file. JPEG-LS defines 3 formats: 1 interchange (most common) and 2 abbreviated.
- Added support for AOT compilation in .NET 8.0 and later.

### Changed

- Internal improvements.
- Improved lossless encoding speed by speedup ratio of 1.2.
- Converted .sln format to .slnx format.
- Updated Nuget package dependencies.

### Fixed

- Forever loop when reading bits from invalid JPEG-LS files in the JpegLSDecoder (found by fuzzing).
- JpegLSDecoder.GetDestinationSize could return negative size instead of throwing InvalidDataException.

## [0.8.0 - 2024-8-23]

### Added

- Initial release.
