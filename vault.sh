sleep 5 &&
curl -X POST 'http://vault:8200/v1/sys/mounts/token-secretstore' -H "Content-Type: application/json" -H "X-Vault-Token: admin" -d '
{
    "type":"kv",
    "config":{
        "force_no_cache":true
    },
    "options":{
        "version":"2"
    }
}
' && sleep 5 &&
curl -X POST 'http://vault:8200/v1/token-secretstore/ServiceConnections' -H "Content-Type: application/json" -H "X-Vault-Token: admin" -d '
{ 
    "DatabaseConnection":"Host=localhost:5432;Database=workflow;Username=postgres;Password=postgres;Include Error Detail=true;",
    "ClientBaseAddress" : "http://localhost:3000/",
    "UserBaseAddress" : "http://localhost:3000/",
    "TagBaseAddress" : "http://localhost:3000/",
    "JwtSecretKey" : "MySuperSecretSuperScretMySuperSecretSuperScretMySuperSecretSuperScretMySuperSecretSuperScretMySuperSecretSuperScret"
}
'