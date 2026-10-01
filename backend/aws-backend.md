AWS Server configuration

1. Am creat un Environment in Elastic Beanstalk
2. am creat o baza de date RDS - PostgreSQL
3. am adaugat reguli de inbound pentru baza de date, dupa IP si server ID: sg-ID
4. am creat o distributie cloudfront pentru server ca sa-l pot muta pe https 
    - unde la origin am specificat Beanstalk, pe protocol HTTP only,
    - la behavior am adaugat GET, POST, etc
    - CacheDisabled
    - Origin request policy - AllViewerExceptHostHeader