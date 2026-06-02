using System;
using System.IO;
using System.Text;

namespace AlaunchX;

public class CryptData
{
	private const string KEYCONTAINER = "GAIA";

	private const string PASSWORD = "Inda";

	private const string PROVIDER = "Microsoft Enhanced Cryptographic Provider v1.0";

	private const int CIPHER_BLOCK = 512;

	public static CryptResult EncryptFile(string fullPath, string targetFile, string password = "Inda")
	{
		string cipherText = null;
		CryptResult result;
		if ((result = EncryptFile(fullPath, ref cipherText, password)) != CryptResult.CryptSuccess)
		{
			return result;
		}
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(targetFile, append: false))
			{
				streamWriter.Write(cipherText);
				streamWriter.Close();
			}
			return CryptResult.CryptSuccess;
		}
		catch
		{
			return CryptResult.CryptSaveFail;
		}
	}

	public static CryptResult EncryptSaveFile(string fullPath, string plainText, string password = "Inda")
	{
		string cipherText = null;
		CryptResult result;
		if ((result = Encrypt(ref cipherText, plainText, password)) != CryptResult.CryptSuccess)
		{
			return result;
		}
		try
		{
			using (StreamWriter streamWriter = new StreamWriter(fullPath, append: false))
			{
				streamWriter.Write(cipherText);
				streamWriter.Close();
			}
			return CryptResult.CryptSuccess;
		}
		catch
		{
			return CryptResult.CryptSaveFail;
		}
	}

	public static CryptResult EncryptFile(string fullPath, ref string cipherText, string password = "Inda")
	{
		if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(password))
		{
			return CryptResult.CryptInvalidParameter;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(fullPath);
			if (!fileInfo.Exists)
			{
				return CryptResult.CryptOpenFail;
			}
			string plainText;
			using (StreamReader streamReader = fileInfo.OpenText())
			{
				plainText = streamReader.ReadToEnd();
				streamReader.Close();
			}
			return Encrypt(ref cipherText, plainText);
		}
		catch
		{
			return CryptResult.CryptOpenFail;
		}
	}

	public static CryptResult DecryptFile(string fullPath, ref string plainText, string password = "Inda")
	{
		if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(password))
		{
			return CryptResult.CryptInvalidParameter;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(fullPath);
			if (!fileInfo.Exists)
			{
				return CryptResult.CryptOpenFail;
			}
			string cipherText;
			using (StreamReader streamReader = fileInfo.OpenText())
			{
				cipherText = streamReader.ReadToEnd();
				streamReader.Close();
			}
			return Decrypt(ref plainText, cipherText, password);
		}
		catch
		{
			return CryptResult.CryptOpenFail;
		}
	}

	public static CryptResult BefCrypt(string password, ref IntPtr hProv, ref IntPtr hHash, ref IntPtr hKey)
	{
		byte[] bytes;
		try
		{
			bytes = Encoding.Unicode.GetBytes(password);
		}
		catch
		{
			return CryptResult.CryptInvalidParameter;
		}
		bool flag = Crypto.CryptAcquireContext(ref hProv, "GAIA", "Microsoft Enhanced Cryptographic Provider v1.0", 1, 0);
		if (!flag)
		{
			flag = Crypto.CryptAcquireContext(ref hProv, "GAIA", "Microsoft Enhanced Cryptographic Provider v1.0", 1, 8);
		}
		if (!flag)
		{
			return CryptResult.CryptAcquirecontextFail;
		}
		if (!Crypto.CryptCreateHash(hProv, 32771, hKey, 0, ref hHash))
		{
			Crypto.CryptReleaseContext(hProv, 0);
			return CryptResult.CryptCreatehashFail;
		}
		if (!Crypto.CryptHashData(hHash, bytes, bytes.Length, 0))
		{
			Crypto.CryptDestroyHash(hHash);
			Crypto.CryptReleaseContext(hProv, 0);
			return CryptResult.CryptHashdataFail;
		}
		if (!Crypto.CryptDeriveKey(hProv, 26115, hHash, 1, ref hKey))
		{
			Crypto.CryptDestroyHash(hHash);
			Crypto.CryptReleaseContext(hProv, 0);
			return CryptResult.CryptDerivekeyFail;
		}
		return CryptResult.CryptSuccess;
	}

	public static CryptResult Encrypt(ref string cipherText, string plainText, string password = "Inda")
	{
		IntPtr hProv = IntPtr.Zero;
		IntPtr hHash = IntPtr.Zero;
		IntPtr hKey = IntPtr.Zero;
		if (plainText == null)
		{
			return CryptResult.CryptInvalidParameter;
		}
		CryptResult result;
		if ((result = BefCrypt(password, ref hProv, ref hHash, ref hKey)) != CryptResult.CryptSuccess)
		{
			return result;
		}
		Crypto.CryptDestroyHash(hHash);
		hHash = IntPtr.Zero;
		try
		{
			byte[] bytes = Encoding.Unicode.GetBytes(plainText);
			uint num = (uint)bytes.Length;
			uint num2 = 0u;
			uint num3 = 0u;
			byte[] array = new byte[2 * num + 1];
			while (num3 < num)
			{
				try
				{
					uint pdwDataLen = 512u;
					byte[] array2 = new byte[1024];
					if (pdwDataLen > num - num3)
					{
						pdwDataLen = num - num3;
					}
					Buffer.BlockCopy(bytes, (int)num3, array2, 0, (int)pdwDataLen);
					num3 += pdwDataLen;
					if (!Crypto.CryptEncrypt(hKey, IntPtr.Zero, (num3 == num) ? 1 : 0, 0u, array2, ref pdwDataLen, 1024u))
					{
						Crypto.CryptDestroyKey(hKey);
						Crypto.CryptReleaseContext(hProv, 0);
						return CryptResult.CryptEncryptFail;
					}
					Buffer.BlockCopy(array2, 0, array, (int)num2, (int)pdwDataLen);
					num2 += pdwDataLen;
				}
				catch
				{
					return CryptResult.CryptInvalidParameter;
				}
			}
			Crypto.CryptDestroyKey(hKey);
			Crypto.CryptReleaseContext(hProv, 0);
			cipherText = Convert.ToBase64String(array, 0, (int)num2);
		}
		catch
		{
			return CryptResult.CryptInvalidParameter;
		}
		return CryptResult.CryptSuccess;
	}

	public static CryptResult Decrypt(ref string plainText, string cipherText, string password = "Inda")
	{
		IntPtr hProv = IntPtr.Zero;
		IntPtr hHash = IntPtr.Zero;
		IntPtr hKey = IntPtr.Zero;
		if (cipherText == null)
		{
			return CryptResult.CryptInvalidParameter;
		}
		CryptResult result;
		if ((result = BefCrypt(password, ref hProv, ref hHash, ref hKey)) != CryptResult.CryptSuccess)
		{
			return result;
		}
		Crypto.CryptDestroyHash(hHash);
		hHash = IntPtr.Zero;
		try
		{
			byte[] array = Convert.FromBase64String(cipherText);
			uint num = 0u;
			uint num2 = 0u;
			uint num3 = (uint)array.Length;
			byte[] array2 = new byte[num3 * 2 + 1];
			while (num2 < num3)
			{
				uint pdwDataLen = 512u;
				byte[] array3 = new byte[512];
				if (pdwDataLen > num3 - num2)
				{
					pdwDataLen = num3 - num2;
				}
				Buffer.BlockCopy(array, (int)num2, array3, 0, (int)pdwDataLen);
				num2 += pdwDataLen;
				if (!Crypto.CryptDecrypt(hKey, IntPtr.Zero, (num2 == num3) ? 1 : 0, 0u, array3, ref pdwDataLen))
				{
					Crypto.CryptDestroyKey(hKey);
					Crypto.CryptReleaseContext(hProv, 0);
					return CryptResult.CryptDecryptFail;
				}
				Buffer.BlockCopy(array3, 0, array2, (int)num, (int)pdwDataLen);
				num += pdwDataLen;
			}
			Crypto.CryptDestroyKey(hKey);
			Crypto.CryptReleaseContext(hProv, 0);
			plainText = Encoding.Unicode.GetString(array2, 0, (int)num);
		}
		catch
		{
			return CryptResult.CryptInvalidParameter;
		}
		return CryptResult.CryptSuccess;
	}
}
