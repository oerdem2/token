sleep 1 &&
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
' &&
curl -X POST 'http://vault:8200/v1/token-secretstore/data/ServiceConnections' -H "Content-Type: application/json" -H "X-Vault-Token: admin" -d '
    {
        "options": {
                "cas": 0
            },
        "data":  {
            "DatabaseConnection": "Host=localhost:5432;Database=token;Username=postgres;Password=postgres",
        }
    }
'