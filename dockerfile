FROM node:14

WORKDIR /usr/src/app
COPY package*.json ./
RUN yarn --prod

COPY . .

RUN yarn build

ENV PORT=8080
EXPOSE 8080

CMD [ "node", "out/main.js" ]
