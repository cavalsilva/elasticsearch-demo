# 📘 Guia de Estudo: Conexão .NET 9 com Elasticsearch 8.x

Este é um projeto de console simples em **.NET 9** para demonstrar a conexão, indexação e busca de documentos em um cluster **Elasticsearch 8.x**.  
O guia inclui a configuração completa do ambiente de desenvolvimento, desde a inicialização dos contêineres Docker até a configuração segura de credenciais usando as melhores práticas do .NET.

---

## 🚀 Pré-requisitos

Antes de começar, garanta que você tenha os seguintes softwares instalados:

- **.NET 9 SDK** (ou superior)  
- **Docker Desktop**  
- **OpenSSL** (já incluso no Git Bash para Windows ou nativo no Linux/macOS)  
- Um terminal de sua preferência (PowerShell, Git Bash, etc.)  

---

## ⚙️ Configuração do Ambiente

Siga estes passos na ordem para configurar todo o ambiente do zero.

### 1. Iniciar os Contêineres (Elasticsearch e Kibana)

**a. Elasticsearch (com segurança habilitada por padrão):**
```bash
docker run -d --name elasticsearch -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" elasticsearch:8.14.1
```
*(Substitua `8.14.1` pela versão mais recente, se desejar)*

**b. Kibana (para visualização de dados):**
```bash
docker run -d --name kibana --link elasticsearch:elasticsearch -p 5601:5601 kibana:8.14.1
```

---

### 2. Configurar as Credenciais de Forma Segura

⚠️ **Atenção**: nunca coloque senhas, fingerprints ou qualquer segredo diretamente no código ou em arquivos `appsettings.json` que serão versionados.  
Usaremos o **User Secrets** do .NET para armazenar essas informações.

**a. Obter a senha do Elasticsearch:**
```bash
docker exec -it elasticsearch bin/elasticsearch-reset-password -u elastic
```

**b. Obter o fingerprint do certificado SSL:**
```bash
docker cp elasticsearch:/usr/share/elasticsearch/config/certs/http_ca.crt .
openssl x509 -noout -fingerprint -sha256 -in http_ca.crt
```
Guarde o valor do fingerprint (ex: `93:8A:66:...`).

**c. Criar `appsettings.json` (somente dados não-sensíveis):**
```json
{
  "Elasticsearch": {
    "Uri": "https://localhost:9200",
    "Username": "elastic"
  }
}
```

**d. Inicializar e salvar no User Secrets:**
```bash
# Inicializa o User Secrets (só precisa rodar uma vez)
dotnet user-secrets init

# Salva senha e fingerprint
dotnet user-secrets set "Elasticsearch:Password" "SUA_SENHA_AQUI"
dotnet user-secrets set "Elasticsearch:Fingerprint" "SEU_FINGERPRINT_AQUI"
```

**e. Garantir que os arquivos sejam lidos pela aplicação:**

1. **Para o `appsettings.json`:**
   - No Visual Studio, vá em: Propriedades > "Copiar para Diretório de Saída" > **Copiar se for mais novo**  
   - Ou adicione no `.csproj`:
     ```xml
     <ItemGroup>
       <Content Include="appsettings.json">
         <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
       </Content>
     </ItemGroup>
     ```

2. **Para o User Secrets:**
   - Crie uma pasta `Properties` na raiz do projeto.
   - Dentro dela, adicione `launchSettings.json`:
     ```json
     {
       "profiles": {
         "ElasticsearchDemo": {
           "commandName": "Project",
           "environmentVariables": {
             "DOTNET_ENVIRONMENT": "Development"
           }
         }
       }
     }
     ```
   *(Substitua `ElasticsearchDemo` pelo nome do seu projeto, se necessário).*

---

## ▶️ Executando a Aplicação

Com tudo configurado, restaure os pacotes e execute o projeto:

```bash
# Restaura pacotes NuGet
dotnet restore

# Executa o projeto
dotnet run
```

O console exibirá mensagens de sucesso para **indexação, busca e deleção de documentos**.

---

## ✅ Verificando no Kibana

Para visualizar os dados no Kibana:

**a. Primeira configuração:**
1. Acesse [http://localhost:5601](http://localhost:5601)  
2. Gere o token de enrollment:
   ```bash
   docker exec -it elasticsearch bin/elasticsearch-create-enrollment-token -s kibana
   ```
3. Cole o token no Kibana.  
4. Gere o código de verificação:
   ```bash
   docker exec -it kibana bin/kibana-verification-code
   ```
5. Cole o código e faça login com usuário `elastic` e a senha definida.

**b. Consultando os dados:**
1. Vá em **Management > Dev Tools**  
2. Execute a query:
   ```json
   GET meus-produtos/_search
   ```
   Se a aplicação já tiver indexado, você verá o documento `"Notebook Gamer"` nos resultados.

---

## 🔧 Troubleshooting (Solução de Problemas)

- **Erro `UntrustedRoot` ou SSL**  
  - **Causa:** Certificado do Elasticsearch é autoassinado  
  - **Solução:** Verifique o `Fingerprint` no User Secrets e garanta que `.CertificateFingerprint(fingerprint)` está no código

- **Erro `401 Unauthorized`**  
  - **Causa:** Senha incorreta  
  - **Solução:** Resete a senha (`elasticsearch-reset-password`) e atualize o User Secrets

- **Configurações não encontradas (variáveis nulas)**  
  - **Causa:** `appsettings.json` não copiado ou ambiente não definido como Development  
  - **Solução:** Verifique as propriedades de cópia e o `launchSettings.json`

- **Erro de conexão ou `ERR_EMPTY_RESPONSE`**  
  - **Causa:** Tentativa de acessar via `http://` em vez de `https://`  
  - **Solução:** Sempre use `https://localhost:9200`

---
