using System.Security.Cryptography;
using System.Text;

namespace Eproc.Services;

public sealed record SealedEnvelopePackage(
    byte[] CiphertextWithTag,
    byte[] NonceIv,
    byte[] WrappedDekCommittee,
    string PayloadSha256Hash,
    DateTimeOffset SealedAtUtc,
    DateTimeOffset ScheduledOpeningUtc
);

/// <summary>
/// Layanan Hybrid Envelope Encryption (AES-256-GCM + RSA-OAEP-SHA256) untuk menyegel:
/// 1. HPS / Owner Estimate (OE) PPK
/// 2. Sampul Penawaran Harga Vendor (1 Sampul / 2 Sampul / 2 Tahap)
/// Dilengkapi Time-Lock Guard sehingga dekripsi ditolak oleh Domain Layer sebelum jadwal pembukaan tiba.
/// </summary>
public sealed class EnvelopeCryptoService
{
    private const int DekSizeBytes = 32; // 256-bit AES key
    private const int NonceSizeBytes = 12; // 96-bit GCM nonce
    private const int TagSizeBytes = 16; // 128-bit GCM auth tag

    /// <summary>
    /// Menyegel payload JSON penawaran atau HPS menggunakan AES-256-GCM dan membungkus DEK dengan RSA Public Key Pokja.
    /// </summary>
    public SealedEnvelopePackage SealEnvelope(
        string plaintextJsonPayload,
        RSA committeePublicKey,
        DateTimeOffset scheduledOpeningUtc,
        DateTimeOffset? currentUtcNow = null)
    {
        if (string.IsNullOrWhiteSpace(plaintextJsonPayload))
            throw new ArgumentException("Payload penawaran/HPS tidak boleh kosong.", nameof(plaintextJsonPayload));

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintextJsonPayload);
        byte[] dek = RandomNumberGenerator.GetBytes(DekSizeBytes);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(dek, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        // Gabungkan Ciphertext + GCM Tag
        byte[] combinedCiphertextAndTag = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combinedCiphertextAndTag, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combinedCiphertextAndTag, ciphertext.Length, tag.Length);

        // Bungkus DEK dengan RSA Public Key milik Komite/Pokja Pemilihan
        byte[] wrappedDek = committeePublicKey.Encrypt(dek, RSAEncryptionPadding.OaepSHA256);

        // Bersihkan DEK dari memori
        CryptographicOperations.ZeroMemory(dek);

        string sha256Hex = Convert.ToHexStringLower(SHA256.HashData(combinedCiphertextAndTag));

        return new SealedEnvelopePackage(
            CiphertextWithTag: combinedCiphertextAndTag,
            NonceIv: nonce,
            WrappedDekCommittee: wrappedDek,
            PayloadSha256Hash: sha256Hex,
            SealedAtUtc: currentUtcNow ?? DateTimeOffset.UtcNow,
            ScheduledOpeningUtc: scheduledOpeningUtc
        );
    }

    /// <summary>
    /// Membuka segel sampul pada saat Bid Opening Ceremony.
    /// Menolak dekripsi apabila jadwal pembukaan belum tiba atau integritas hash SHA-256 berubah.
    /// </summary>
    public string OpenSealedEnvelope(
        SealedEnvelopePackage sealedPackage,
        RSA committeePrivateKey,
        DateTimeOffset currentCeremonyUtc,
        bool isTechnicalStagePassed = true)
    {
        if (!isTechnicalStagePassed)
        {
            throw new InvalidOperationException(
                "Eproc:BidOpening:TechnicalNotPassed - Sampul Harga (Sampul 2) dilarang dibuka untuk peserta yang gugur pada evaluasi Administrasi/Teknis.");
        }

        if (currentCeremonyUtc < sealedPackage.ScheduledOpeningUtc)
        {
            throw new InvalidOperationException(
                $"Eproc:BidOpening:ScheduleLocked - Sampul terenkripsi tidak dapat dibuka sebelum jadwal resmi ({sealedPackage.ScheduledOpeningUtc:O}).");
        }

        // Verifikasi integritas ciphertext terhadap manipulasi database
        string actualHash = Convert.ToHexStringLower(SHA256.HashData(sealedPackage.CiphertextWithTag));
        if (!string.Equals(actualHash, sealedPackage.PayloadSha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new CryptographicException(
                "Eproc:BidOpening:IntegrityViolation - Hash SHA-256 sampul penawaran tidak cocok! Terdeteksi indikasi perubahan data ilegal.");
        }

        byte[] dek = committeePrivateKey.Decrypt(sealedPackage.WrappedDekCommittee, RSAEncryptionPadding.OaepSHA256);

        try
        {
            int ciphertextLength = sealedPackage.CiphertextWithTag.Length - TagSizeBytes;
            byte[] ciphertext = new byte[ciphertextLength];
            byte[] tag = new byte[TagSizeBytes];

            Buffer.BlockCopy(sealedPackage.CiphertextWithTag, 0, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(sealedPackage.CiphertextWithTag, ciphertextLength, tag, 0, TagSizeBytes);

            byte[] decryptedBytes = new byte[ciphertextLength];
            using var aesGcm = new AesGcm(dek, TagSizeBytes);
            aesGcm.Decrypt(sealedPackage.NonceIv, ciphertext, tag, decryptedBytes);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }
}
