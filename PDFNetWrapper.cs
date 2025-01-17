﻿using System;

using pdftron;
using pdftron.Crypto;
using pdftron.PDF;
using pdftron.PDF.Annots;
using pdftron.SDF;

namespace azure_hsm_signing
{
  public class PDFNetWrapper {
    private static DigitalSignatureField certification_sig_field;
    public PDFNetWrapper(string licenseKey) {
      Console.WriteLine($"PDFNet Version: {PDFNet.GetVersionString()}");
      PDFNet.Initialize(licenseKey);
    }
    public PDFDoc PreparePdfForCustomSigning(PDFDoc doc, string signatureFieldName, uint sizeOfContents = 7500)
    {
      Field found_approval_field = doc.GetField(signatureFieldName);
      bool isLockedByDigitalSignature = found_approval_field != null && found_approval_field.IsLockedByDigitalSignature();

      if (isLockedByDigitalSignature)
      {
        throw new Exception($"The field {signatureFieldName} is locked by a Digital Signature, and thus cannot be Digitally Signed again");
      }

      certification_sig_field = new DigitalSignatureField(found_approval_field);

      certification_sig_field.SetDocumentPermissions(DigitalSignatureField.DocumentPermissions.e_no_changes_allowed);

      // Prepare the signature and signature handler for signing.
      certification_sig_field.CreateSigDictForCustomSigning(
          "Adobe.PPKLite",
          // This chosen enum assumes you wish to use a PADES compliant signing mode
          // Please see this documentation for all possible options
          // https://www.pdftron.com/api/PDFTronSDK/dotnet/pdftron.PDF.DigitalSignatureField.SubFilterType.html
          DigitalSignatureField.SubFilterType.e_ETSI_CAdES_detached,
          sizeOfContents
      );

      doc.Save(SDFDoc.SaveOptions.e_incremental);
      return doc;
    }

    public DigitalSignatureField GetDigitalSignatureField() {
      return certification_sig_field;
    }

    public byte[] GetPdfDigest(DigestAlgorithm.Type digestAlgorithm = DigestAlgorithm.Type.e_sha256)
    {
      return certification_sig_field.CalculateDigest(digestAlgorithm);
    }

    public void SavePdfWithDigitalSignature(PDFDoc doc, string signatureFieldName, byte[] pkcs7message, string pdfDirectory, string nameOfFile)
    {
      Field certification_field = doc.GetField(signatureFieldName);
      DigitalSignatureField certification_sig_field = new DigitalSignatureField(certification_field);
      doc.SaveCustomSignature(
        pkcs7message,
        certification_sig_field,
        $"{pdfDirectory}{nameOfFile}"
      );
    }
    public pdftron.Crypto.X509Certificate CreatePdftronX509Certificate(byte[] certificateInPemFormat)
    {
      X509Certificate[] chain_certs = new X509Certificate[] { new X509Certificate(certificateInPemFormat) };
      return new pdftron.Crypto.X509Certificate(certificateInPemFormat);
    }
  }
}
