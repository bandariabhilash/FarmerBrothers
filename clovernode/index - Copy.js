const express = require('express');
const request = require('request');
const cors = require('cors')

const app = express();
const PORT = process.env.PORT || 7503;

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
      capture:req.body.capture
    };
    console.log(data);

    var BodyData ={
      amount: data?.amount, 
      source: data?.source,
      currency: data?.currency,
      capture:req.body.capture
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
