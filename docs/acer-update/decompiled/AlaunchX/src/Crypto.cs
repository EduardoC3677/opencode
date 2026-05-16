using System;
using System.Runtime.InteropServices;

namespace AlaunchX;

public class Crypto
{
	public struct PUBLICKEYSTRUC
	{
		public byte bType;

		public byte bVersion;

		public short reserved;

		public int aiKeyAlg;
	}

	public struct RSAPUBKEY
	{
		public int magic;

		public int bitlen;

		public int pubexp;
	}

	public struct CRYPTOAPI_BLOB
	{
		public int cbData;

		public byte[] pbData;
	}

	public struct CRYPT_ALGORITHM_IDENTIFIER
	{
		[MarshalAs(UnmanagedType.LPStr)]
		public string pszObjId;

		public CRYPTOAPI_BLOB Parameters;
	}

	public struct CRYPT_SIGN_MESSAGE_PARA
	{
		public int cbSize;

		public int dwMsgEncodingType;

		public IntPtr pSigningCert;

		public CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;

		public IntPtr pvHashAuxInfo;

		public int cMsgCert;

		public IntPtr rgpMsgCert;

		public int cMsgCrl;

		public IntPtr rgpMsgCrl;

		public int cAuthAttr;

		public IntPtr rgAuthAttr;

		public int cUnauthAttr;

		public IntPtr rgUnauthAttr;

		public int dwFlags;

		public int dwInnerContentType;
	}

	public struct CRYPT_VERIFY_MESSAGE_PARA
	{
		public int cbSize;

		public int dwMsgAndCertEncodingType;

		public IntPtr hCryptProv;

		public IntPtr pfnGetSignerCertificate;

		public IntPtr pvGetArg;
	}

	public struct CRYPT_BIT_BLOB
	{
		public int cbData;

		public IntPtr pbData;

		public int cUnusedBits;
	}

	public struct CERT_PUBLIC_KEY_INFO
	{
		public CRYPT_ALGORITHM_IDENTIFIER Algorithm;

		public CRYPT_BIT_BLOB PublicKey;
	}

	public const int PRIVATEKEYBLOB = 7;

	public const int AT_KEYEXCHANGE = 1;

	public const int AT_SIGNATURE = 2;

	public const int CRYPT_E_NOT_FOUND = -2146885628;

	public const string CERT_PERSONAL_STORE_NAME = "My";

	public const int CERT_COMPARE_ANY = 0;

	public const int CERT_COMPARE_NAME = 2;

	public const int CERT_INFO_SUBJECT_FLAG = 7;

	public const int CERT_COMPARE_SHIFT = 16;

	public const int CERT_FIND_SUBJECT_NAME = 131079;

	public const int CERT_FIND_ANY = 0;

	public const int CERT_COMPARE_NAME_STR_W = 8;

	public const int CERT_FIND_SUBJECT_STR_W = 524295;

	public const int CERT_FIND_SUBJECT_STR = 524295;

	public const int CERT_STORE_PROV_SYSTEM_W = 10;

	public const int CERT_STORE_PROV_SYSTEM = 10;

	public const int CERT_SYSTEM_STORE_CURRENT_USER_ID = 1;

	public const int CERT_SYSTEM_STORE_LOCATION_SHIFT = 16;

	public const int CERT_SYSTEM_STORE_CURRENT_USER = 65536;

	public const int CERT_CLOSE_STORE_CHECK_FLAG = 2;

	public const int CALG_SHA1 = 32772;

	public const int CALG_MD5 = 32771;

	public const int CALG_3DES = 26115;

	public const int CALG_RSA_SIGN = 9216;

	public const int PROV_RSA_FULL = 1;

	public const int CRYPT_VERIFYCONTEXT = -268435456;

	public const int CRYPT_NEWKEYSET = 8;

	public const int CRYPT_DELETEKEYSET = 16;

	public const int CRYPT_MACHINE_KEYSET = 32;

	public const int CRYPT_SILENT = 64;

	public const int X509_ASN_ENCODING = 1;

	public const int PKCS_7_ASN_ENCODING = 65536;

	public const int MY_TYPE = 65537;

	public const int HP_HASHVAL = 2;

	public const int HP_HASHSIZE = 4;

	public const int PUBLICKEYBLOBEX = 10;

	public const int PUBLICKEYBLOB = 6;

	public const int CUR_BLOB_VERSION = 2;

	public const int CRYPT_EXPORTABLE = 1;

	public const string szOID_RSA_RSA = "1.2.840.113549.1.1.1";

	public const string szOID_RSA_MD5 = "1.2.840.113549.2.5";

	public const string szOID_RSA_MD5RSA = "1.2.840.113549.1.1.4";

	public const string szOID_OIWSEC_sha1 = "1.3.14.3.2.26";

	public const int RSA_CSP_PUBLICKEYBLOB = 19;

	public const int X509_PUBLIC_KEY_INFO = 8;

	[DllImport("Crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr CertOpenStore(int lpszStoreProvider, int dwMsgAndCertEncodingType, IntPtr hCryptProv, int dwFlags, string pvPara);

	[DllImport("Crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr CertOpenSystemStore(IntPtr hprov, string szSubsystemProtocol);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CertCloseStore(IntPtr hCertStore, int dwFlags);

	[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptAcquireContext(ref IntPtr hProv, string pszContainer, string pszProvider, int dwProvType, int dwFlags);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptCreateHash(IntPtr hProv, int Algid, IntPtr hKey, int dwFlags, ref IntPtr phHash);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CryptDeriveKey(IntPtr hProv, int Algid, IntPtr hBaseData, int dwFlags, ref IntPtr phKey);

	[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CryptEncrypt(IntPtr hKey, IntPtr hHash, int Final, uint dwFlags, byte[] pbData, ref uint pdwDataLen, uint dwBufLen);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CryptDecrypt(IntPtr hKey, IntPtr hHash, int Final, uint dwFlags, byte[] pbData, ref uint pdwDataLen);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptGetHashParam(IntPtr hHash, int dwParam, ref int pbData, ref int pdwDataLen, int dwFlags);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptSetHashParam(IntPtr hHash, int dwParam, byte[] pbData, int dwFlags);

	[DllImport("crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptImportPublicKeyInfo(IntPtr hCryptProv, int dwCertEncodingType, IntPtr pInfo, ref IntPtr phKey);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptImportKey(IntPtr hProv, byte[] pbData, int dwDataLen, IntPtr hPubKey, int dwFlags, ref IntPtr phKey);

	[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CryptVerifySignature(IntPtr hHash, byte[] pbSignature, int dwSigLen, IntPtr hPubKey, string sDescription, int dwFlags);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptDestroyKey(IntPtr hKey);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptDestroyHash(IntPtr hHash);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptReleaseContext(IntPtr hProv, int dwFlags);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptGenKey(IntPtr hProv, int Algid, int dwFlags, ref IntPtr phKey);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool CryptExportKey(IntPtr hKey, IntPtr hExpKey, int dwBlobType, int dwFlags, byte[] pbData, ref int pdwDataLen);

	[DllImport("Crypt32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr CertFindCertificateInStore(IntPtr hCertStore, int dwCertEncodingType, int dwFindFlags, int dwFindType, string pvFindPara, IntPtr pPrevCertContext);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CertFreeCertificateContext(IntPtr pCertContext);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CryptSignMessage(ref CRYPT_SIGN_MESSAGE_PARA pSignPara, bool fDetachedSignature, int cToBeSigned, IntPtr[] rgpbToBeSigned, int[] rgcbToBeSigned, byte[] pbSignedBlob, ref int pcbSignedBlob);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CryptVerifyMessageSignature(ref CRYPT_VERIFY_MESSAGE_PARA pVerifyPara, int dwSignerIndex, byte[] pbSignedBlob, int cbSignedBlob, byte[] pbDecoded, ref int pcbDecoded, IntPtr ppSignerCert);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CryptEncodeObject(int dwCertEncodingType, int lpszStructType, ref CERT_PUBLIC_KEY_INFO pvStructInfo, byte[] pbEncoded, ref int pcbEncoded);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CryptEncodeObject(int dwCertEncodingType, int lpszStructType, byte[] pvStructInfo, IntPtr pbEncoded, ref int pcbEncoded);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern IntPtr CertCreateCertificateContext(int dwCertEncodingType, byte[] pbCertEncoded, int cbCertEncoded);

	[DllImport("Crypt32.dll", SetLastError = true)]
	public static extern bool CryptAcquireCertificatePrivateKey(IntPtr pCert, int dwFlags, IntPtr pvReserved, ref IntPtr phCryptProv, ref int pdwKeySpec, ref bool pfCallerFreeProv);

	[DllImport("Advapi32.dll", SetLastError = true)]
	public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, int dwDataLen, int dwFlags);

	[DllImport("Advapi32.dll", SetLastError = true)]
	public static extern bool CryptSignHash(IntPtr hHash, int dwKeySpec, string sDescription, int dwFlags, byte[] pbSignature, ref int pdwSigLen);

	[DllImport("Advapi32.dll", SetLastError = true)]
	public static extern bool CryptGetUserKey(IntPtr hProv, int dwKeySpec, ref IntPtr phUserKey);
}
