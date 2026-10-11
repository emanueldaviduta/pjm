aws sts get-caller-identity
{
    "UserId": "755145267615",
    "Account": "755145267615",
    "Arn": "arn:aws:iam::755145267615:root"
}


1. build & archive
dotnet publish -c Release -o ./publish
cd publish && zip -r ../app-v2.zip . && cd ..

2. Upload în S3
aws s3 cp app-v1.zip s3://elasticbeanstalk-eu-central-1-755145267615/app-v2.zip

3. Creezi o versiune a aplicației
aws elasticbeanstalk create-application-version \
  --application-name PJM \
  --version-label app-269 \
  --source-bundle S3Bucket=elasticbeanstalk-eu-central-1-755145267615,S3Key=app-v2.zip

4. Deploy pe environment
aws elasticbeanstalk update-environment \
  --environment-name PJM-staging \
  --version-label app-269



UI Deploy

npm run build:prod

aws s3 sync ./dist/pjm/browser s3://pjm-s3-bucket-7953/ --delete \
  --cache-control "no-cache"

aws cloudfront create-invalidation --distribution-id E212C1PNZNG5IJ --paths "/*"

DB

aws rds stop-db-instance --db-instance-identifier pjm-database

aws rds start-db-instance --db-instance-identifier pjm-database