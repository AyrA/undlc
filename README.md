# undlc

This application decrypts DLC files

## How to use

Run `undlc file.dlc` and it will dump a list to the console.
You can specify multiple files to extract all links from them.

### Arguments

undlc supports two arguments:

- `/F` If supplied, full output is provided. Instead of only Links, the output is `Link<tab>Filename<tab>Filesize`
- `/JSON` If supplied, the output is a JSON array of Links. If combined with `/F`, the entire DLC structure is dumped to console.

undlc will not attempt to validate the file name for your local system or check if the file size is valid,
it merely extracts these properties from the DLC file. Filesize is almost always going to be zero.

## How DLC works

DLC has been set up in a way that users depend on a server.
The client side part is just sloppy usage of AES.

### Encryption

1. Generate an 8 byte key
2. Convert the key to ASCII hexadecimal representation to get 16 bytes
3. Send the Hex key to JD Service to get a "fake key".
4. Encrypt your DLC file with the Hex key. Use Key as IV too
5. Store Encrypted Data + Fake Key

As you see, you don't necessarily need to form a DLC file and can use the service to encrypt any sort of file.
It's not known at that point if the Fake key is purely random or has an actual meaning.
it's always 88 B64 chars long, or 66 decoded bytes.

Because only this fake key is stored with the encrypted content,
it's not possible to decrypt the file without contacting a server for the real key first.

### Decryption

1. Split the Data and Fake Key apart
2. Send Fake Key to JD Service to get the Real Key as ASCII Hex
3. Convert Real Key from ASCII Hex to Bytes to get 16 byte AES Key
4. Decrypt Key using Hardcoded Key and IV to get the Original Key
5. Use Original Key to decrypt Encrypted Data

## Creating DLC

The `DlcContainer.cs` contains everything needed to create DLC files.
This application is not using these methods however.

This application is not using these methods however.