\# Guia de Estudo: Conexão .NET 9 com Elasticsearch 8.x



Este é um projeto de console simples em .NET 9 para demonstrar a conexão, indexação e busca de documentos em um cluster Elasticsearch 8.x. O guia inclui a configuração completa do ambiente de desenvolvimento, desde a inicialização dos contêineres Docker até a configuração segura de credenciais usando as melhores práticas do .NET.



---



\## 🚀 Pré-requisitos



Antes de começar, garanta que você tenha os seguintes softwares instalados:



\* \*\*.NET 9 SDK\*\* (ou superior)

\* \*\*Docker Desktop\*\*

\* \*\*OpenSSL\*\* (geralmente já vem com o Git Bash para Windows ou nativamente no Linux/macOS)

\* Um terminal de sua preferência (PowerShell, Git Bash, etc.)



---



\## ⚙️ Configuração do Ambiente



Siga estes passos na ordem para configurar todo o ambiente do zero.



\### 1. Iniciar os Contêineres (Elasticsearch e Kibana)



Execute os seguintes comandos no seu terminal para iniciar os contêineres necessários.



\*\*a. Elasticsearch (com segurança habilitada por padrão):\*\*

```bash

docker run -d --name elasticsearch -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" elasticsearch:8.14.1

```

\*(Substitua `8.14.1` pela versão estável mais recente, se desejar)\*



\*\*b. Kibana (para visualização de dados):\*\*

```bash

docker run -d --name kibana --link elasticsearch:elasticsearch -p 5601:5601 kibana:8.14.1

```



\### 2. Configurar as Credenciais de Forma Segura



\*\*NUNCA\*\* coloque senhas, fingerprints ou qualquer segredo diretamente no código ou em arquivos `appsettings.json` que serão enviados para o Git. Usaremos a ferramenta \*\*User Secrets\*\* do .NET para isso.



\*\*a. Obter a Senha do Elasticsearch:\*\*

A forma mais segura de garantir a senha correta é resetá-la. Execute o comando abaixo e guarde a nova senha gerada.

```bash

docker exec -it elasticsearch bin/elasticsearch-reset-password -u elastic

```



\*\*b. Obter o Fingerprint do Certificado SSL:\*\*

Primeiro, copie o certificado do contêiner para sua máquina local.

```bash

docker cp elasticsearch:/usr/share/elasticsearch/config/certs/http\_ca.crt .

```

Depois, gere o fingerprint a partir do arquivo.

```bash

openssl x509 -noout -fingerprint -sha256 -in http\_ca.crt

```

Guarde o valor do fingerprint (ex: `93:8A:66:...`).



\*\*c. Configurar o `appsettings.json` (Dados não-sensíveis):\*\*

Crie um arquivo `appsettings.json` na raiz do projeto com o seguinte conteúdo:

```json

{

&nbsp; "Elasticsearch": {

&nbsp;   "Uri": "https://localhost:9200",

&nbsp;   "Username": "elastic"

&nbsp; }

}

```



\*\*d. Inicializar e Salvar no User Secrets:\*\*

No terminal, na pasta do projeto, execute os seguintes comandos para salvar suas credenciais de forma segura.



```bash

\# Inicializa o User Secrets para o projeto (só precisa rodar uma vez)

dotnet user-secrets init



\# Salva a senha e o fingerprint

dotnet user-secrets set "Elasticsearch:Password" "SUA\_SENHA\_GERADA\_AQUI"

dotnet user-secrets set "Elasticsearch:Fingerprint" "SEU\_FINGERPRINT\_GERADO\_AQUI"

```



\*\*e. Garantir que os Arquivos de Configuração sejam Lidos:\*\*

Para que a aplicação encontre os arquivos de configuração ao ser executada, siga os passos:

1\.  \*\*Para o `appsettings.json`\*\*:

&nbsp;   \* No Visual Studio, clique com o botão direito no arquivo `appsettings.json` > Propriedades > "Copiar para Diretório de Saída" > "Copiar se for mais novo".

&nbsp;   \* Ou adicione no `.csproj`:

&nbsp;       ```xml

&nbsp;       <ItemGroup>

&nbsp;         <Content Include="appsettings.json">

&nbsp;           <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>

&nbsp;         </Content>

&nbsp;       </ItemGroup>

&nbsp;       ```

2\.  \*\*Para o User Secrets\*\*:

&nbsp;   \* Crie uma pasta `Properties` na raiz do projeto.

&nbsp;   \* Dentro dela, crie um arquivo `launchSettings.json` com o seguinte conteúdo para definir o ambiente como "Development":

&nbsp;       ```json

&nbsp;       {

&nbsp;         "profiles": {

&nbsp;           "ElasticsearchDemo": {

&nbsp;             "commandName": "Project",

&nbsp;             "environmentVariables": {

&nbsp;               "DOTNET\_ENVIRONMENT": "Development"

&nbsp;             }

&nbsp;           }

&nbsp;         }

&nbsp;       }

&nbsp;       ```

&nbsp;       \*(Ajuste "ElasticsearchDemo" para o nome do seu projeto, se for diferente).\*



---



\## ▶️ Executando a Aplicação



Com toda a configuração pronta, restaure os pacotes e execute a aplicação:

```bash

\# Restaura os pacotes NuGet (boa prática)

dotnet restore



\# Executa o projeto

dotnet run

```

O console deverá exibir as mensagens de sucesso para indexação, busca e deleção do documento.



---



\## ✅ Verificando no Kibana



Para visualizar o que sua aplicação está fazendo, use o Kibana.



\*\*a. Primeira Configuração do Kibana:\*\*

1\.  Acesse `http://localhost:5601`.

2\.  Ele pedirá um \*\*Enrollment Token\*\*. Gere-o no terminal:

&nbsp;   ```bash

&nbsp;   docker exec -it elasticsearch bin/elasticsearch-create-enrollment-token -s kibana

&nbsp;   ```

3\.  Cole o token no Kibana.

4\.  Ele pedirá um \*\*Verification Code\*\*. Gere-o no terminal:

&nbsp;   ```bash

&nbsp;   docker exec -it kibana bin/kibana-verification-code

&nbsp;   ```

5\.  Cole o código e faça login com o usuário `elastic` e a senha que você definiu.



\*\*b. Verificando os Dados:\*\*

1\.  No menu do Kibana, vá em \*\*Management > Dev Tools\*\*.

2\.  Execute a seguinte query para buscar todos os documentos no índice `meus-produtos`:

&nbsp;   ```json

&nbsp;   GET meus-produtos/\_search

&nbsp;   ```

&nbsp;   Se você rodar este comando enquanto a aplicação .NET está pausada (com um breakpoint) após a indexação, verá o documento "Notebook Gamer" nos resultados.



---



\## 🔧 Guia de Solução de Problemas (Troubleshooting)



\* \*\*Erro `UntrustedRoot` ou de Certificado SSL:\*\*

&nbsp;   \* \*\*Causa:\*\* O certificado do Elasticsearch é auto-assinado.

&nbsp;   \* \*\*Solução:\*\* Garanta que o `Fingerprint` está correto no User Secrets e que a linha `.CertificateFingerprint(fingerprint)` está no seu código C#.



\* \*\*Erro `401 Unauthorized`:\*\*

&nbsp;   \* \*\*Causa:\*\* A senha está incorreta.

&nbsp;   \* \*\*Solução:\*\* Reset a senha no Docker (`elasticsearch-reset-password`) e atualize o User Secret (`dotnet user-secrets set ...`).



\* \*\*Configurações não encontradas (variáveis nulas no C#):\*\*

&nbsp;   \* \*\*Causa:\*\* O `appsettings.json` não está sendo copiado para a pasta de build ou o ambiente não está como "Development" para ler os User Secrets.

&nbsp;   \* \*\*Solução:\*\* Verifique a propriedade "Copiar para Diretório de Saída" do `appsettings.json` e a configuração do `launchSettings.json`.



\* \*\*Erro de Conexão ou `ERR\_EMPTY\_RESPONSE` no navegador:\*\*

&nbsp;   \* \*\*Causa:\*\* Tentando acessar Elasticsearch via `http://` quando ele espera `https://`.

&nbsp;   \* \*\*Solução:\*\* Use sempre `https://localhost:9200`.

