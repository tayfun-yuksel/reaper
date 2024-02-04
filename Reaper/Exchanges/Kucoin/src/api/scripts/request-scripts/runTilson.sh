nohup curl -X 'GET' \
  'http://localhost:5177/Strategy/Tilson?symbol=dmailusdtm&amount=66&profitPercentage=0.015&interval=5' \
  -H 'accept: */*' &> ~/projects/reaper/Reaper/Exchanges/Kucoin/src/api/tilson.log &
