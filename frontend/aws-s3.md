AWS Configurations

1. Am creat un S3 Bucket
2. Am dezactivat: Block public access 
3. Am adaugat policy:  {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::pjm-s3-bucket-7953/*"
        }

CloudFront
1. Am adaugat o distributie
2. Am adaugat: Default root object -> index.html
3. Am adaugat Error page: 403 redirect pe /index.html cu response 200.

