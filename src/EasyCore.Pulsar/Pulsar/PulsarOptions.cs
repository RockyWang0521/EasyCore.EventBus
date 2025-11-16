using Pulsar.Client.Api;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace EasyCore.Pulsar
{
    /// <summary>
    /// Pulsar connection, TLS, authentication, and topic naming options.
    /// </summary>
    public class PulsarOptions
    {
        /// <summary>
        /// Service URL, e.g. <c>pulsar://localhost:6650</c>.
        /// </summary>
        public string ServiceUrl { get; set; } = "pulsar://localhost:6650";

        /// <summary>
        /// When <c>true</c>, enables client-side logging from the Pulsar client library.
        /// </summary>
        public bool EnableClientLog { get; set; } = false;

        /// <summary>
        /// Whether TLS is used for the Pulsar connection.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public bool UseTls { get; set; } = PulsarClientConfiguration.Default.UseTls;

        /// <summary>
        /// Whether TLS hostname verification is enabled.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public bool TlsHostnameVerificationEnable { get; set; } = PulsarClientConfiguration.Default.TlsHostnameVerificationEnable;

        /// <summary>
        /// Whether insecure TLS connections are allowed.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public bool TlsAllowInsecureConnection { get; set; } = PulsarClientConfiguration.Default.TlsAllowInsecureConnection;

        /// <summary>
        /// Trust certificate used for TLS validation.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public X509Certificate2 TlsTrustCertificate { get; set; } = PulsarClientConfiguration.Default.TlsTrustCertificate;

        /// <summary>
        /// Pulsar authentication plugin/configuration.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public Authentication Authentication { get; set; } = PulsarClientConfiguration.Default.Authentication;

        /// <summary>
        /// Allowed SSL/TLS protocol versions.
        /// Defaults to <see cref="PulsarClientConfiguration.Default"/>.
        /// </summary>
        public SslProtocols TlsProtocols { get; set; } = PulsarClientConfiguration.Default.TlsProtocols;

        /// <summary>
        /// Topic namespace prefix applied to relative topic names.
        /// Default is <c>persistent://public/default/</c>.
        /// </summary>
        public string TopicPrefix { get; set; } = "persistent://public/default/";

        /// <summary>
        /// Application name used for subscription and consumer naming.
        /// When null, the entry assembly name is used.
        /// </summary>
        public string? AppName { get; set; }
    }
}
