@api
Feature: Product API

Scenario: Get an existing product
  When I request the product with id 1
  Then the response status is 200
  And the product has name "Ryzen 7 7800X3D" and price 399

Scenario: Delete a product
  Given a product exists with the following details
    | Name         | Description          | Price | ProductType |
    | API Delete 1 | Created by API tests |    10 | CPU         |
  When I delete the created product
  Then the response status is 204
  When I request the created product
  Then the response status is 404
