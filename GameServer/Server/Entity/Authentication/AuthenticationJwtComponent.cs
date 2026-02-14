using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

namespace Fantasy;

public class AuthenticationJwtComponent : Entity
{
    /// <summary>
    /// RSA公钥PEM格式字符串，用于JWT令牌验证
    /// </summary>
    public string PublicKeyPem = "MIIBCgKCAQEAqc1moV1nynBYVXhsXeTuDc/DcC7TSQ+dVmTsyeUZP+PaiZjxp/cIo0sj9OCAcAK2nCBpIjBjM7Rbg7Uslb4KvlH8eshsH8LOL9KybNsB0uFg/kPvSvIBcdWsxbbARxDGrTl+Nh1t6s2V1voK9g+OQqK/xIyhvrwFYIjc1/O6FI4uGyMag3D6MJATW2SCrwe1tKU9dfk72VrIqT4FVzbZUDQdFxEKHiQ4vcoBiGxsoYWr6lxbrrtDDNzPzVEgzDbsvaPvSCNRacFaBCJBeGCWGn/fKKBOl5NV2EfXa8oNFgw0VfC4JiRpPUjdhsKvzjtp5P3AYrnloGkTxp9HLkdDjwIDAQAB";

    /// <summary>
    /// RSA私钥PEM格式字符串，用于JWT令牌签名
    /// </summary>
    public string PrivateKeyPem = "MIIEowIBAAKCAQEAqc1moV1nynBYVXhsXeTuDc/DcC7TSQ+dVmTsyeUZP+PaiZjxp/cIo0sj9OCAcAK2nCBpIjBjM7Rbg7Uslb4KvlH8eshsH8LOL9KybNsB0uFg/kPvSvIBcdWsxbbARxDGrTl+Nh1t6s2V1voK9g+OQqK/xIyhvrwFYIjc1/O6FI4uGyMag3D6MJATW2SCrwe1tKU9dfk72VrIqT4FVzbZUDQdFxEKHiQ4vcoBiGxsoYWr6lxbrrtDDNzPzVEgzDbsvaPvSCNRacFaBCJBeGCWGn/fKKBOl5NV2EfXa8oNFgw0VfC4JiRpPUjdhsKvzjtp5P3AYrnloGkTxp9HLkdDjwIDAQABAoIBAAP7djm4mBOOWPQANAJruQ2H3tyFrJzdCeKZ0VfW0KXH8Tsi5B/9Ui2KOJionzqBRckZlX2gxuLSiwbmu9nzMAtQnuw+QRLpna5PcbHN3RgWAxFThIwsas5BpaCpbY79HLu5SnV4cTh9g9Mb4B/vM3XqnTa92ZlAjZu6+fryk12iAiWKGgMIJDG1TW7PLz9bAdhvfLcJ4ipP+dDtbbYuDSkykijYS7Vq94syhD00HCnPWeNb/UQBwYoD/rsQ9j0zTVNe5SAMhNVm2vC+TX38pYDzGahNo6Y5RnKQAPC7WchEhmesuQYaxZo+2ZtquN9OC2WanaHzoT5ru9yUXY6n9ikCgYEA7FtQXFaeAI7J2pSsb5LnKoGZsWnHBhLUMVUKEGx2ORk37kAbUBY1gNsRuIysEmpkWTFfLCWoqFEca/gK/79viggrwbkjzizZOICXONEgDm5YRK+0K728FGuPWCxj6J+nrcstBKem/zPxt7gqVXeCJWw9DAjySbsWeZ8nAQtDCTMCgYEAt+oWcLQLO/J46k8HA4euavrcDYrY5qCxiq/E7CeV7h9g5Cb/oMMICqNASlzL80uynLkCaVTvs+v25ZJeZU/Pyty9jNbYDtOxnABBNZNolrrgtw2cB47m93/3X1ABn0eJ451l0AEbBu5y/MoLEDdCD7s1FtvRZ0FEOynuLKZYNDUCgYAnUGUEhu/PHfEbZ4FrKZ9D5by/0t1k+DrNXdzHsJLVmolAGu+wGZGatlWzQcrZYVMBtwGioTz1ilBU8TQ7KAeQiR1mIrp+79zN1i3RKI/Rdq899Y/derjPGLkGLJQNvIiyksoRLSXM60H4kiyQfMlsGh3pY4+RjgoF/uAI/2uOGwKBgC/uWhrVUXg2IT0vi2xoGBTZfDArV4wDpUG3vWKujmyY0O+lGkoTiiz7ju3ScXTe7ZXawb4h3LbPcHE0TFWrD0SXcu8ZrwVuNoOprVUBLejam2YZrehqXddzCk1U1JdmVZF0m+wx3ZFY467uGSQIwrbG23cxosl+QQIKU4Bfee1hAoGBAJhnrD5GJmqrTQEZta9gjIhtHPoIWxwwYCkxbq/j2N7GLudlLzAs304ByqlKNNYysT9SahPS8oGvqdRFd2bo90MQy0L7U2z6SG+gNhX7EGzDU6AnpotuVy56r/9bLRW4rXrHgwz8dUJWjqmmIBpsjAhpIykBu3HefSoiMc0s3RvJ";

    /// <summary>
    /// JWT签名凭据，用于生成JWT令牌
    /// </summary>
    public SigningCredentials SigningCredentials;

    /// <summary>
    /// JWT令牌验证参数，用于验证传入的JWT令牌
    /// </summary>
    public TokenValidationParameters TokenValidationParameters;
}