using Pulsar.Client.Api;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace EasyCore.EventBus.Pulsar
{
    public class PulsarOptions
    {
        /// <summary>
        /// Service Url
        /// </summary>
        public string ServiceUrl { get; set; } = "localhost:6650";

        /// <summary>
        /// Enable Client Log
        /// </summary>
        public bool EnableClientLog { get; set; } = false;

        /// <summary>
        /// Use Tls
        /// </summary>
        public bool UseTls { get; set; } = PulsarClientConfiguration.Default.UseTls;

        /// <summary>
        /// Tls Hostname Verification Enable
        /// </summary>
        public bool TlsHostnameVerificationEnable { get; set; } = PulsarClientConfiguration.Default.TlsHostnameVerificationEnable;

        /// <summary>
        /// Tls AllowInsecure Connection
        /// </summary>
        public bool TlsAllowInsecureConnection { get; set; } = PulsarClientConfiguration.Default.TlsAllowInsecureConnection;

        /// <summary>
        /// Tls Trust Certificate
        /// </summary>
        public X509Certificate2 TlsTrustCertificate { get; set; } = PulsarClientConfiguration.Default.TlsTrustCertificate;

        /// <summary>
        /// Authentication
        /// </summary>
        public Authentication Authentication { get; set; } = PulsarClientConfiguration.Default.Authentication;

        /// <summary>
        /// Tls Protocols
        /// </summary>
        public SslProtocols TlsProtocols { get; set; } = PulsarClientConfiguration.Default.TlsProtocols;
    }
}
