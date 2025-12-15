const express = require('express');
const request = require('request');
const cors = require('cors')

const app = express();
const PORT = process.env.PORT || 7500;

app.use(express.json());
app.use(cors());
app.get('/', function(req, res, next) {
      res.send('Payment Api to  /charge')
})
// Endpoint to forward charge requests to Clover API
app.post('/charge', function(req, res, next) {
    var data = {
      amount: req.body.amount, 
      source: req.body.source,
      currency: req.body.currency,
      apiurl:req.body.apiurl,
      btoken:req.body.btoken,
      capture:req.body.capture,
      receipt_email:req.body.receipt_email,
      external_reference_id:req.body.external_reference_id,
      external_customer_reference:req.body.external_customer_reference,
      ecomind:req.body.ecomind,
      metadata:req.body.metadata,
    };
    
    var BodyData ={
      amount: data?.amount, 
      source: data?.source,
      currency: data?.currency,
      capture:req.body.capture,
      external_reference_id:data?.external_reference_id,
      external_customer_reference:data?.external_customer_reference,
      ecomind:data?.ecomind,
      metadata:data?.metadata,
    }

    if(req.body.receipt_email != null && req.body.receipt_email !== "" && req.body.receipt_email !== undefined) {
      BodyData.receipt_email = data?.receipt_email
    }
    // Post a charge from merchant server to clover server with api auth token
    request.post(`${data?.apiurl}`, {
        json: BodyData,
        headers: {
          "accept": "application/json",
          "authorization": `Bearer ${data?.btoken}`,
          "content-type": "application/json"
        }
      }, (error, response, body) => {
        if (error) {
            res.status(500).send({error: 'Failed to process the charge'});
            return;
        }
        res.send(body);
    });
  });

// Start the server
app.listen(PORT, () => {
    console.log(`Server is running on port http://localhost:${PORT}`);
});
